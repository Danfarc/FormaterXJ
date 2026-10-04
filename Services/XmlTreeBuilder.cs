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

    public DocumentNode BuildExplorerRoot(string xmlText, DocumentNode elementRoot)
    {
        var document = XDocument.Parse(xmlText);
        var root = new DocumentNode
        {
            Name = "XML",
            Path = "/",
            Type = "Documento XML"
        };

        if (document.Declaration is { } declaration)
        {
            root.AddChild(new DocumentNode
            {
                Name = "xml",
                Path = "/?xml",
                Type = "Declaración XML",
                Value = declaration.ToString()
            });
        }

        root.Children.Add(elementRoot);
        return root;
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
