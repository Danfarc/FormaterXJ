using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FormaterXJ.Models;
using FormaterXJ.Services;

namespace FormaterXJ.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string inputText = string.Empty;

    [ObservableProperty]
    private string lineNumbersText = "1";

    [ObservableProperty]
    private double editorFontSize = 10;

    [ObservableProperty]
    private double treeFontSize = 13;

    [ObservableProperty]
    private double treeExpanderSize = 12;

    [ObservableProperty]
    private double treeItemMinHeight = 28;

    [ObservableProperty]
    private Thickness treeItemPadding = new(4);

    [ObservableProperty]
    private Thickness treeExpanderMargin = new(12, 0);

    [ObservableProperty]
    private CornerRadius treeHeaderCornerRadius = new(4);

    [ObservableProperty]
    private Thickness treeHeaderBorderThickness;

    [ObservableProperty]
    private Thickness treeItemMargin;

    [ObservableProperty]
    private string treeStyle = "Standard";

    [ObservableProperty]
    private bool showLineNumbers = true;

    [ObservableProperty]
    private bool isTreeOnLeft;

    [ObservableProperty]
    private string theme = "Dark";

    [ObservableProperty]
    private string profileStatus = string.Empty;

    [ObservableProperty]
    private string formattedText = string.Empty;

    [ObservableProperty]
    private string detectedFormat = "Sin formato";

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool searchMatchCase;

    [ObservableProperty]
    private bool searchWholeWord;

    [ObservableProperty]
    private int searchSelectionStart = -1;

    [ObservableProperty]
    private int searchSelectionLength;

    [ObservableProperty]
    private string searchStatus = "Sin búsqueda";

    [ObservableProperty]
    private bool hasValidDocument;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private int errorLine;

    [ObservableProperty]
    private int errorColumn;

    [ObservableProperty]
    private int errorPosition = -1;

    [ObservableProperty]
    private int errorNavigationRequest;

    [ObservableProperty]
    private ObservableCollection<DocumentNode> rootNodes = new();

    [ObservableProperty]
    private DocumentNode? selectedNode;

    [ObservableProperty]
    private string selectedNodeDetail = string.Empty;

    private DocumentNode? rootDocument;
    private IReadOnlyList<DocumentNode> searchMatches = Array.Empty<DocumentNode>();
    private IReadOnlyList<SearchResult> searchResults = Array.Empty<SearchResult>();
    private int currentSearchIndex;
    private bool applyingSearchResult;

    private sealed record SearchResult(DocumentNode Node, int Start, int Length);

    public bool IsDarkTheme => Theme == "Dark";

    public bool IsLightTheme => Theme == "Light";

    public bool IsSystemTheme => Theme == "System";

    public bool IsStandardTreeStyle => TreeStyle == "Standard";

    public bool IsCompactTreeStyle => TreeStyle == "Compact";

    public bool IsCardTreeStyle => TreeStyle == "Cards";

    public bool HasProfileStatus => !string.IsNullOrEmpty(ProfileStatus);

    public bool HasErrorLocation => ErrorPosition >= 0;

    public int DocumentPanelColumn => IsTreeOnLeft ? 2 : 0;

    public int TreePanelColumn => IsTreeOnLeft ? 0 : 2;

    public bool IsTreeOnRight
    {
        get => !IsTreeOnLeft;
        set => IsTreeOnLeft = !value;
    }

    partial void OnIsTreeOnLeftChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTreeOnRight));
        OnPropertyChanged(nameof(DocumentPanelColumn));
        OnPropertyChanged(nameof(TreePanelColumn));
    }

    partial void OnThemeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsSystemTheme));
    }

    partial void OnTreeStyleChanged(string value)
    {
        OnPropertyChanged(nameof(IsStandardTreeStyle));
        OnPropertyChanged(nameof(IsCompactTreeStyle));
        OnPropertyChanged(nameof(IsCardTreeStyle));
        UpdateTreeStyleMetrics();
    }

    partial void OnProfileStatusChanged(string value)
    {
        OnPropertyChanged(nameof(HasProfileStatus));
    }

    partial void OnErrorPositionChanged(int value)
    {
        OnPropertyChanged(nameof(HasErrorLocation));
    }

    public void ApplyProfile(UserProfile profile)
    {
        EditorFontSize = profile.EditorFontSize;
        TreeFontSize = profile.TreeFontSize;
        ShowLineNumbers = profile.ShowLineNumbers;
        IsTreeOnLeft = profile.IsTreeOnLeft;
        Theme = profile.Theme;
        TreeStyle = profile.TreeStyle;
    }

    public UserProfile CreateProfile()
    {
        return new UserProfile
        {
            EditorFontSize = EditorFontSize,
            TreeFontSize = TreeFontSize,
            ShowLineNumbers = ShowLineNumbers,
            IsTreeOnLeft = IsTreeOnLeft,
            Theme = Theme,
            TreeStyle = TreeStyle
        };
    }

    partial void OnErrorMessageChanged(string value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }

    partial void OnInputTextChanged(string value)
    {
        var lineCount = Math.Max(1, value.Count(character => character == '\n') + 1);
        LineNumbersText = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount));
        ValidateDocumentInput(value);
    }

    private void ValidateDocumentInput(string input)
    {
        var analysis = DocumentFormatter.Analyze(input);
        if (analysis.IsValid)
        {
            ErrorMessage = string.Empty;
            ErrorLine = 0;
            ErrorColumn = 0;
            ErrorPosition = -1;
            return;
        }

        ErrorMessage = analysis.ErrorMessage;
        ErrorLine = analysis.ErrorLine;
        ErrorColumn = analysis.ErrorColumn;
        ErrorPosition = analysis.ErrorPosition;
    }

    partial void OnSearchTextChanged(string value)
    {
        RunSearch();
    }

    partial void OnSearchMatchCaseChanged(bool value)
    {
        RunSearch();
    }

    partial void OnSearchWholeWordChanged(bool value)
    {
        RunSearch();
    }

    partial void OnSelectedNodeChanged(DocumentNode? value)
    {
        if (!applyingSearchResult)
        {
            SearchSelectionStart = -1;
            SearchSelectionLength = 0;
        }
    }

    partial void OnTreeFontSizeChanged(double value)
    {
        UpdateTreeStyleMetrics();
    }

    private void UpdateTreeStyleMetrics()
    {
        var scale = TreeFontSize / 13;
        switch (TreeStyle)
        {
            case "Compact":
                TreeExpanderSize = 10 * scale;
                TreeItemMinHeight = 20 * scale;
                TreeItemPadding = new Thickness(2 * scale);
                TreeExpanderMargin = new Thickness(6 * scale, 0);
                TreeHeaderCornerRadius = new CornerRadius(2 * scale);
                TreeHeaderBorderThickness = new Thickness(0);
                TreeItemMargin = new Thickness(0);
                break;
            case "Cards":
                TreeExpanderSize = 13 * scale;
                TreeItemMinHeight = 38 * scale;
                TreeItemPadding = new Thickness(8 * scale);
                TreeExpanderMargin = new Thickness(12 * scale, 0);
                TreeHeaderCornerRadius = new CornerRadius(8 * scale);
                TreeHeaderBorderThickness = new Thickness(1);
                TreeItemMargin = new Thickness(0, 2 * scale);
                break;
            default:
                TreeExpanderSize = 12 * scale;
                TreeItemMinHeight = 28 * scale;
                TreeItemPadding = new Thickness(4 * scale);
                TreeExpanderMargin = new Thickness(12 * scale, 0);
                TreeHeaderCornerRadius = new CornerRadius(4 * scale);
                TreeHeaderBorderThickness = new Thickness(0);
                TreeItemMargin = new Thickness(0);
                break;
        }
    }

    [RelayCommand]
    private void ExpandAllNodes()
    {
        SetAllNodesExpanded(true);
    }

    [RelayCommand]
    private void CollapseAllNodes()
    {
        SetAllNodesExpanded(false);
    }

    private void SetAllNodesExpanded(bool isExpanded)
    {
        if (rootDocument is null)
        {
            return;
        }

        foreach (var node in Flatten(rootDocument))
        {
            node.IsExpanded = isExpanded;
        }
    }

    [RelayCommand]
    private void FormatDocument()
    {
        ClearSearchState();
        rootDocument = null;
        ErrorMessage = string.Empty;
        ErrorLine = 0;
        ErrorColumn = 0;
        ErrorPosition = -1;
        HasValidDocument = false;
        FormattedText = string.Empty;
        DetectedFormat = "Sin formato";
        SelectedNode = null;
        SelectedNodeDetail = string.Empty;
        RootNodes.Clear();

        if (string.IsNullOrWhiteSpace(InputText))
        {
            ErrorMessage = "El contenido está vacío. Pega o carga un documento JSON o XML válido.";
            return;
        }

        var analysis = DocumentFormatter.Analyze(InputText);
        if (!analysis.IsValid)
        {
            ErrorMessage = analysis.ErrorMessage;
            ErrorLine = analysis.ErrorLine;
            ErrorColumn = analysis.ErrorColumn;
            ErrorPosition = analysis.ErrorPosition;
            if (ErrorPosition >= 0)
            {
                ErrorNavigationRequest++;
            }

            return;
        }

        HasValidDocument = true;
        DetectedFormat = analysis.DetectLabel;
        FormattedText = analysis.FormattedText;

        if (analysis.Kind == DocumentFormatKind.Json)
        {
            rootDocument = new JsonTreeBuilder().Build(InputText);
        }
        else if (analysis.Kind == DocumentFormatKind.Xml)
        {
            rootDocument = new XmlTreeBuilder().Build(InputText);
        }
        else
        {
            throw new InvalidOperationException("Formato no soportado.");
        }

        InputText = analysis.FormattedText;
        FormattedText = analysis.FormattedText;
        DocumentTextLocator.Locate(rootDocument, analysis.FormattedText, analysis.Kind);
        RootNodes.Clear();
        RootNodes.Add(rootDocument);
        SelectedNode = rootDocument;
        SelectedNodeDetail = rootDocument.DetailText;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            RunSearch();
        }
        else
        {
            SearchStatus = "Documento listo para buscar";
        }
    }

    [RelayCommand]
    private void SearchNext()
    {
        if (rootDocument is null || string.IsNullOrWhiteSpace(SearchText))
        {
            SearchStatus = string.IsNullOrWhiteSpace(SearchText) ? "Escribe un texto para buscar." : "Sin coincidencias";
            return;
        }

        if (searchResults.Count == 0)
        {
            SearchStatus = "Sin coincidencias";
            return;
        }

        currentSearchIndex = (currentSearchIndex + 1) % searchResults.Count;
        ApplyMatch(currentSearchIndex);
    }

    [RelayCommand]
    private void SearchPrevious()
    {
        if (rootDocument is null || string.IsNullOrWhiteSpace(SearchText))
        {
            SearchStatus = string.IsNullOrWhiteSpace(SearchText) ? "Escribe un texto para buscar." : "Sin coincidencias";
            return;
        }

        if (searchResults.Count == 0)
        {
            SearchStatus = "Sin coincidencias";
            return;
        }

        currentSearchIndex = (currentSearchIndex - 1 + searchResults.Count) % searchResults.Count;
        ApplyMatch(currentSearchIndex);
    }

    public void LoadText(string text)
    {
        InputText = text;
        FormattedText = string.Empty;
        DetectedFormat = "Sin formato";
        HasValidDocument = false;
        ErrorMessage = string.Empty;
        rootDocument = null;
        RootNodes.Clear();
        SelectedNode = null;
        SelectedNodeDetail = string.Empty;
        ClearSearchState();
        FormatDocument();
    }

    public void SelectNodeAtEditorPosition(int position)
    {
        if (rootDocument is null || InputText.Length == 0 || InputText != FormattedText)
        {
            return;
        }

        var safePosition = Math.Clamp(position, 0, InputText.Length);
        var searchStart = Math.Min(safePosition, InputText.Length - 1);
        var lineStart = InputText.LastIndexOf('\n', searchStart);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        var lineEnd = InputText.IndexOf('\n', safePosition);
        if (lineEnd < 0)
        {
            lineEnd = InputText.Length;
        }

        var candidate = Flatten(rootDocument)
            .Where(node => node.EditorStart >= lineStart && node.EditorStart < lineEnd)
            .OrderBy(node => node.EditorStart)
            .LastOrDefault(node => node.EditorStart <= safePosition);

        candidate ??= Flatten(rootDocument)
            .Where(node => node.EditorStart >= 0)
            .OrderBy(node => Math.Abs(node.EditorStart - safePosition))
            .FirstOrDefault();

        if (candidate is null)
        {
            return;
        }

        var parent = candidate.Parent;
        while (parent is not null)
        {
            parent.IsExpanded = true;
            parent = parent.Parent;
        }

        SelectedNode = candidate;
        SelectedNodeDetail = candidate.DetailText;
    }

    public void RunSearch()
    {
        if (rootDocument is null)
        {
            searchMatches = Array.Empty<DocumentNode>();
            searchResults = Array.Empty<SearchResult>();
            currentSearchIndex = 0;
            SearchStatus = "Primero formatea un documento.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ClearSearchState();
            searchMatches = Array.Empty<DocumentNode>();
            searchResults = Array.Empty<SearchResult>();
            currentSearchIndex = 0;
            SearchSelectionStart = -1;
            SearchSelectionLength = 0;
            SearchStatus = "Sin búsqueda";
            return;
        }

        searchMatches = SearchService.FindMatches(
            rootDocument,
            SearchText,
            SearchMatchCase,
            SearchWholeWord);
        var results = new List<SearchResult>();
        foreach (var textMatch in SearchService.FindTextMatches(
                     FormattedText,
                     SearchText,
                     SearchMatchCase,
                     SearchWholeWord))
        {
            var node = FindNodeForTextMatch(textMatch.Start, textMatch.Length);
            if (node is not null)
            {
                results.Add(new SearchResult(node, textMatch.Start, textMatch.Length));
            }
        }

        foreach (var nodeMatch in searchMatches)
        {
            var nodeStart = nodeMatch.EditorBlockStart >= 0
                ? nodeMatch.EditorBlockStart
                : nodeMatch.EditorStart;
            var nodeLength = nodeMatch.EditorBlockStart >= 0
                ? nodeMatch.EditorBlockLength
                : nodeMatch.EditorLength;
            var hasDocumentMatch = results.Any(result =>
                ReferenceEquals(result.Node, nodeMatch)
                || (nodeStart >= 0
                    && result.Start >= nodeStart
                    && result.Start + result.Length <= nodeStart + nodeLength));

            if (!hasDocumentMatch && nodeStart >= 0 && nodeLength > 0)
            {
                results.Add(new SearchResult(nodeMatch, nodeStart, nodeLength));
            }
        }

        searchResults = results.OrderBy(result => result.Start).ToList();
        if (searchResults.Count == 0)
        {
            ClearSearchState();
            currentSearchIndex = 0;
            SearchSelectionStart = -1;
            SearchSelectionLength = 0;
            SearchStatus = "Sin coincidencias";
            return;
        }

        currentSearchIndex = 0;
        ApplyMatch(currentSearchIndex);
    }

    private void ApplyMatch(int index)
    {
        if (rootDocument is null || searchResults.Count == 0)
        {
            return;
        }

        ClearSearchState();

        var result = searchResults[index];
        var selected = result.Node;
        selected.IsSearchMatch = true;
        selected.IsCurrentSearchMatch = true;

        var current = selected.Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }

        applyingSearchResult = true;
        SelectedNode = selected;
        applyingSearchResult = false;
        SelectedNodeDetail = selected.DetailText;
        SearchSelectionStart = result.Start;
        SearchSelectionLength = result.Length;
        SearchStatus = $"Coincidencia {index + 1} de {searchResults.Count}";
    }

    private DocumentNode? FindNodeForTextMatch(int start, int length)
    {
        if (rootDocument is null)
        {
            return null;
        }

        return Flatten(rootDocument)
            .Where(node => node.EditorBlockStart >= 0
                && start >= node.EditorBlockStart
                && start + length <= node.EditorBlockStart + node.EditorBlockLength)
            .OrderBy(node => node.EditorBlockLength)
            .FirstOrDefault()
            ?? rootDocument;
    }

    private void ClearSearchState()
    {
        if (rootDocument is null)
        {
            return;
        }

        foreach (var item in Flatten(rootDocument))
        {
            item.IsSearchMatch = false;
            item.IsCurrentSearchMatch = false;
        }
    }

    private static IEnumerable<DocumentNode> Flatten(DocumentNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var nested in Flatten(child))
            {
                yield return nested;
            }
        }
    }
}
