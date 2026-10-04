using System;
using System.Collections.ObjectModel;
using System.Text.Json;
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

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isDarkTheme;

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

    public string ExplorerIcon => Type switch
    {
        "Object" => "{}",
        "Array" => "[]",
        "Elemento" => "◇",
        _ => "■"
    };

    public double ExplorerIconFontSize => Type is "Object" or "Array" ? 21 : 12;

    public IBrush ExplorerIconBrush => Type switch
    {
        "Object" or "Array" => CreateBrush(IsDarkTheme ? "#8AB4F8" : "#263FBE"),
        "Number" => CreateBrush(IsDarkTheme ? "#81C995" : "#218A43"),
        "String" => CreateBrush(IsDarkTheme ? "#80CBC4" : "#2447A8"),
        "True" or "False" => CreateBrush(IsDarkTheme ? "#FFB86C" : "#A05A00"),
        "Null" => CreateBrush(IsDarkTheme ? "#B0B8C1" : "#727985"),
        _ => CreateBrush(IsDarkTheme ? "#C4A7E7" : "#52637A")
    };

    public IBrush ExplorerForegroundBrush => CreateBrush(IsDarkTheme ? "#F1F3F4" : "#202124");

    public string ExplorerLabel
    {
        get
        {
            if (Path == "$")
            {
                return "JSON";
            }

            var name = Name.StartsWith("[", StringComparison.Ordinal)
                && Name.EndsWith("]", StringComparison.Ordinal)
                    ? Name[1..^1]
                    : Name;

            if (Type is "Object" or "Array")
            {
                return name;
            }

            var value = Type == "String"
                ? JsonSerializer.Serialize(Value)
                : Value;
            return string.IsNullOrEmpty(name) ? value : $"{name}: {value}";
        }
    }

    public string XmlExplorerIcon => Type switch
    {
        "Documento XML" => "XML",
        "Declaración XML" => "<??>",
        "Atributo" => "@",
        "Elemento" when Parent is null => "ROOT",
        "Elemento" => "<>",
        _ => "TXT"
    };

    public IBrush XmlExplorerIconBrush => Type switch
    {
        "Documento XML" => CreateBrush(IsDarkTheme ? "#8AB4F8" : "#263FBE"),
        "Declaración XML" => CreateBrush(IsDarkTheme ? "#C4A7E7" : "#7040A0"),
        "Atributo" => CreateBrush(IsDarkTheme ? "#FFB86C" : "#A05A00"),
        "Elemento" when Parent is null => CreateBrush(IsDarkTheme ? "#81C995" : "#218A43"),
        "Elemento" => CreateBrush(IsDarkTheme ? "#80CBC4" : "#087F8C"),
        _ => CreateBrush(IsDarkTheme ? "#B0B8C1" : "#727985")
    };

    public string XmlExplorerLabel => Type switch
    {
        "Documento XML" => "Documento XML",
        "Declaración XML" => Value,
        "Atributo" => $"Atributo: {Name.TrimStart('@')}",
        "Elemento" when Parent is null => $"Elemento raíz: <{Name}>",
        "Elemento" => $"Elemento: <{Name}>",
        _ => Name
    };

    public string XmlExplorerContentLabel => string.IsNullOrWhiteSpace(Value)
        ? string.Empty
        : $"Contenido: {Value}";

    public bool HasXmlExplorerContent => !string.IsNullOrWhiteSpace(Value)
        && Type == "Elemento";

    public IBrush SearchBackground => IsCurrentSearchMatch
        ? CreateBrush(IsDarkTheme ? "#765B16" : "#FFD54F")
        : IsSearchMatch
            ? CreateBrush(IsDarkTheme ? "#514521" : "#B8860B")
            : IsSelected
                ? CreateBrush(IsDarkTheme ? "#354D6B" : "#D9E8FA")
                : Brushes.Transparent;

    partial void OnIsSearchMatchChanged(bool value) => OnPropertyChanged(nameof(SearchBackground));

    partial void OnIsCurrentSearchMatchChanged(bool value) => OnPropertyChanged(nameof(SearchBackground));

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(SearchBackground));
        OnPropertyChanged(nameof(ExplorerForegroundBrush));
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        OnPropertyChanged(nameof(ExplorerIconBrush));
        OnPropertyChanged(nameof(ExplorerForegroundBrush));
        OnPropertyChanged(nameof(XmlExplorerIconBrush));
        OnPropertyChanged(nameof(XmlExplorerForegroundBrush));
        OnPropertyChanged(nameof(XmlExplorerContentBrush));
        OnPropertyChanged(nameof(SearchBackground));
    }

    public IBrush XmlExplorerForegroundBrush => CreateBrush(IsDarkTheme ? "#F1F3F4" : "#202124");

    public IBrush XmlExplorerContentBrush => CreateBrush(IsDarkTheme ? "#FFCC80" : "#8A4B08");

    private static IBrush CreateBrush(string color) => new SolidColorBrush(Color.Parse(color));

    public void AddChild(DocumentNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
