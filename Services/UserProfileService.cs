using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FormaterXJ.Models;

namespace FormaterXJ.Services;

public sealed class UserProfileService
{
    private static readonly double[] AvailableFontSizes = [10, 12, 13, 14, 16, 18];
    private readonly string profilesDirectory;
    private readonly string activeProfilePath;

    public UserProfileService(string profilesDirectory)
    {
        this.profilesDirectory = profilesDirectory;
        activeProfilePath = Path.Combine(profilesDirectory, "active-profile.txt");
    }

    public static string DefaultProfilesDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FormaterXJ",
        "Profiles");

    public static bool IsValidProfileName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name)
            && name.Length <= 50
            && name.Trim() == name
            && name.All(character => char.IsLetterOrDigit(character) || character is ' ' or '-' or '_');
    }

    public IReadOnlyList<string> ListProfiles()
    {
        if (!Directory.Exists(profilesDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(profilesDirectory, "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Where(name => IsValidProfileName(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public UserProfile? LoadActiveProfile()
    {
        if (!File.Exists(activeProfilePath))
        {
            return null;
        }

        var name = File.ReadAllText(activeProfilePath);
        return Load(name);
    }

    public UserProfile Load(string name)
    {
        var profile = Read(name);
        SetActiveProfile(name);
        return profile;
    }

    public UserProfile Read(string name)
    {
        var profilePath = GetProfilePath(name);
        var profile = JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(profilePath))
            ?? throw new InvalidDataException("El perfil está vacío o no contiene una configuración válida.");
        if (!string.Equals(profile.Name, name, StringComparison.Ordinal))
        {
            throw new InvalidDataException("El nombre del perfil no coincide con su archivo.");
        }
        Validate(profile);
        Validate(profile);
        return profile;
    }

    public void Save(string name, UserProfile profile)
    {
        if (!IsValidProfileName(name))
        {
            throw new InvalidDataException("El nombre debe tener hasta 50 caracteres y usar solo letras, números, espacios, guiones o guiones bajos.");
        }

        profile.Name = name;
        Validate(profile);
        Directory.CreateDirectory(profilesDirectory);
        WriteAtomically(GetProfilePath(name), JsonSerializer.Serialize(profile, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
        SetActiveProfile(name);
    }

    public void Delete(string name)
    {
        var profilePath = GetProfilePath(name);
        if (!File.Exists(profilePath))
        {
            throw new FileNotFoundException("No se encontró el perfil seleccionado.", profilePath);
        }

        File.Delete(profilePath);
        if (File.Exists(activeProfilePath)
            && string.Equals(File.ReadAllText(activeProfilePath), name, StringComparison.Ordinal))
        {
            File.Delete(activeProfilePath);
        }
    }

    private string GetProfilePath(string name)
    {
        if (!IsValidProfileName(name))
        {
            throw new InvalidDataException("El nombre del perfil no es válido.");
        }

        return Path.Combine(profilesDirectory, name + ".json");
    }

    private void SetActiveProfile(string name)
    {
        if (!Directory.Exists(profilesDirectory))
        {
            Directory.CreateDirectory(profilesDirectory);
        }

        WriteAtomically(activeProfilePath, name);
    }

    private static void WriteAtomically(string path, string contents)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, contents);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void Validate(UserProfile profile)
    {
        if (!IsValidProfileName(profile.Name)
            || !Array.Exists(AvailableFontSizes, size => size == profile.EditorFontSize)
            || !Array.Exists(AvailableFontSizes, size => size == profile.TreeFontSize)
            || profile.Theme is not ("Dark" or "Light" or "System")
            || profile.TreeStyle is not ("Standard" or "Compact" or "Cards" or "Explorer" or "ExplorerXml"))
        {
            throw new InvalidDataException("El perfil contiene valores no compatibles.");
        }
    }
}
