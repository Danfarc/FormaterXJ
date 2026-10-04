using System.Linq;
using System.Text.Json;
using FormaterXJ.Models;

namespace FormaterXJ.Services;

public sealed class JsonTreeBuilder
{
    public DocumentNode Build(string jsonText)
    {
        using var document = JsonDocument.Parse(jsonText);

        return BuildNode("Documento", document.RootElement, "$", true);
    }

    private static DocumentNode BuildNode(string name, JsonElement element, string path, bool isRoot = false)
    {
        var node = new DocumentNode
        {
            Name = string.IsNullOrWhiteSpace(name) ? (isRoot ? "Documento" : "Valor") : name,
            Path = path,
            Type = element.ValueKind.ToString(),
            Value = GetValueText(element)
        };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    node.AddChild(BuildNode(property.Name, property.Value, GetPropertyPath(path, property.Name)));
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    node.AddChild(BuildNode($"[{index}]", item, path + $"[{index}]"));
                    index++;
                }
                break;
        }

        if (node.Children.Count > 0)
        {
            node.Value = string.Empty;
            node.IsExpanded = true;
        }

        return node;
    }

    private static string GetPropertyPath(string parentPath, string propertyName)
    {
        if (propertyName.Length > 0
            && (char.IsLetter(propertyName[0]) || propertyName[0] is '_' or '$')
            && propertyName.Skip(1).All(character =>
                char.IsLetterOrDigit(character) || character is '_' or '$'))
        {
            return $"{parentPath}.{propertyName}";
        }

        return $"{parentPath}['{propertyName.Replace("\\", "\\\\").Replace("'", "\\'")}']";
    }

    private static string GetValueText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => string.Empty,
            JsonValueKind.Array => string.Empty,
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }
}
