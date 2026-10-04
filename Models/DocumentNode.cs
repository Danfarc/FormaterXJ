using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FormaterXJ.Models;

public partial class DocumentNode : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

    [ObservableProperty]
    private bool isExpanded = true;

    [ObservableProperty]
    private bool isSearchMatch;

    [ObservableProperty]
    private bool isCurrentSearchMatch;
    public int EditorStart { get; set; } = -1;
    public int EditorLength { get; set; }
    public int EditorBlockStart { get; set; } = -1;
    public int EditorBlockLength { get; set; }
    public DocumentNode? Parent { get; set; }
    public ObservableCollection<DocumentNode> Children { get; } = new();

    public string DisplayText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var text = string.IsNullOrWhiteSpace(Name) ? Value : $"{Name}: {Value}";
                return Children.Count > 0 ? $"{text} ({Children.Count})" : text;
            }

            if (Children.Count > 0)
            {
                return string.IsNullOrWhiteSpace(Name) ? $"({Children.Count})" : $"{Name} ({Children.Count})";
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

    partial void OnIsSearchMatchChanged(bool value) => OnPropertyChanged(nameof(SearchBackground));

    partial void OnIsCurrentSearchMatchChanged(bool value) => OnPropertyChanged(nameof(SearchBackground));

    public void AddChild(DocumentNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
