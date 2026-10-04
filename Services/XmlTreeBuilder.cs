using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FormaterXJ.Models;

namespace FormaterXJ.Services;

public sealed class XmlTreeBuilder
{
    public DocumentNode Build(string xmlText)
    {
        var document = XDocument.Parse(xmlText);
        var root = document.Root ?? throw new InvalidOperationException("El documento XML no tiene raíz.");
        return BuildNode(root, $"/{root.Name.LocalName}");
    }

    private static DocumentNode BuildNode(XElement element, string path)
    {
        var node = new DocumentNode
        {
            Name = element.Name.LocalName,
            Path = path,
            Type = "Elemento",
            Value = GetTextValue(element)
        };

        foreach (var attribute in element.Attributes())
        {
            var attributeNode = new DocumentNode
            {
                Name = $"@{attribute.Name.LocalName}",
                Path = $"{path}/@{attribute.Name.LocalName}",
                Type = "Atributo",
                Value = attribute.Value
            };

            node.AddChild(attributeNode);
        }

        var childElements = element.Elements().ToList();
        var sameNameCounts = childElements
            .GroupBy(child => child.Name.LocalName)
            .ToDictionary(group => group.Key, group => group.Count());
        var sameNameIndexes = new Dictionary<string, int>();
        foreach (var child in childElements)
        {
            var childName = child.Name.LocalName;
            var childPath = $"{path}/{childName}";
            if (sameNameCounts[childName] > 1)
            {
                sameNameIndexes.TryGetValue(childName, out var index);
                index++;
                sameNameIndexes[childName] = index;
                childPath += $"[{index}]";
            }

            node.AddChild(BuildNode(child, childPath));
        }

        if (element.HasElements)
        {
            node.Value = string.Empty;
        }

        var textNodes = element.Nodes().OfType<XText>()
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Value.Trim())
            .ToList();

        if (textNodes.Count > 0 && !element.HasElements)
        {
            node.Value = string.Join(" ", textNodes);
        }

        return node;
    }

    private static string GetTextValue(XElement element)
    {
        if (!element.HasElements)
        {
            return element.Value.Trim();
        }

        return string.Empty;
    }
}
