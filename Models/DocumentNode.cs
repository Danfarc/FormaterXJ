using System;
using System.Collections.ObjectModel;
using Avalonia.Media;

namespace FormaterXJ.Models;

public class DocumentNode
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsExpanded { get; set; } = true;
    public bool IsSearchMatch { get; set; }
    public bool IsCurrentSearchMatch { get; set; }
    public DocumentNode? Parent { get; set; }
    public ObservableCollection<DocumentNode> Children { get; } = new();

    public string DisplayText
    {
        get
        {
            if (Children.Count > 0)
            {
                return string.IsNullOrWhiteSpace(Name) ? $"({Children.Count})" : $"{Name} ({Children.Count})";
            }

            if (!string.IsNullOrWhiteSpace(Value))
            {
                return string.IsNullOrWhiteSpace(Name) ? Value : $"{Name}: {Value}";
            }

            return string.IsNullOrWhiteSpace(Name) ? "Nodo" : Name;
        }
    }

    public string DetailText => string.IsNullOrWhiteSpace(Value)
        ? $"Nombre: {Name}{Environment.NewLine}Ruta: {Path}{Environment.NewLine}Tipo: {Type}"
        : $"Nombre: {Name}{Environment.NewLine}Valor: {Value}{Environment.NewLine}Ruta: {Path}{Environment.NewLine}Tipo: {Type}";

    public IBrush SearchBackground => IsCurrentSearchMatch
        ? Brushes.Gold
        : IsSearchMatch
            ? Brushes.DarkGoldenrod
            : Brushes.Transparent;

    public void AddChild(DocumentNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
