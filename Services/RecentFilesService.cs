using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FormaterXJ.Services;

public sealed class RecentFilesService
{
    public const int MaximumRecentFiles = 10;

    private readonly string recentFilesPath;

    public RecentFilesService(string recentFilesPath)
    {
        this.recentFilesPath = recentFilesPath;
    }

    public static string DefaultRecentFilesPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FormaterXJ",
        "recent-files.json");

    public IReadOnlyList<string> Load()
    {
        if (!File.Exists(recentFilesPath))
        {
            return Array.Empty<string>();
        }

        var paths = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(recentFilesPath))
            ?? throw new InvalidDataException("La lista de archivos recientes está vacía o dañada.");
        return paths
            .Where(IsSupportedFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumRecentFiles)
            .ToArray();
    }

    public void Add(string path)
    {
        if (!IsSupportedFile(path))
        {
            throw new InvalidDataException("Solo se admiten archivos JSON, XML o TXT.");
        }

        var fullPath = Path.GetFullPath(path);
        var paths = Load()
            .Where(recentPath => !string.Equals(recentPath, fullPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        paths.Insert(0, fullPath);

        var directory = Path.GetDirectoryName(recentFilesPath)
            ?? throw new InvalidOperationException("No se pudo determinar la carpeta de archivos recientes.");
        Directory.CreateDirectory(directory);
        var temporaryPath = recentFilesPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(paths.Take(MaximumRecentFiles)));
            File.Move(temporaryPath, recentFilesPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static bool IsSupportedFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return Path.GetExtension(path) is var extension
            && (extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase));
    }
}
