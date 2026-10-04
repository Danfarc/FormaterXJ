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
        return BuildNode(document.Root ?? throw new InvalidOperationException("El documento XML no tiene raíz."));
    }

    private static DocumentNode BuildNode(XElement element)
    {
        var node = new DocumentNode
        {
            Name = element.Name.LocalName,
            Path = element.Name.LocalName,
            Type = "Elemento",
            Value = GetTextValue(element)
        };

        foreach (var attribute in element.Attributes())
        {
            var attributeNode = new DocumentNode
            {
                Name = $"@{attribute.Name.LocalName}",
                Path = $"{element.Name.LocalName}/@{attribute.Name.LocalName}",
                Type = "Atributo",
                Value = attribute.Value
            };

            node.AddChild(attributeNode);
        }

        foreach (var child in element.Elements())
        {
            node.AddChild(BuildNode(child));
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
