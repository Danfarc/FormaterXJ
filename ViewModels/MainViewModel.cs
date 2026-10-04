using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    private double editorFontSize = 13;

    [ObservableProperty]
    private double treeFontSize = 13;

    [ObservableProperty]
    private bool showLineNumbers = true;

    [ObservableProperty]
    private string formattedText = string.Empty;

    [ObservableProperty]
    private string detectedFormat = "Sin formato";

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string searchStatus = "Sin búsqueda";

    [ObservableProperty]
    private bool hasValidDocument;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private ObservableCollection<DocumentNode> rootNodes = new();

    [ObservableProperty]
    private DocumentNode? selectedNode;

    [ObservableProperty]
    private string selectedNodeDetail = string.Empty;

    private DocumentNode? rootDocument;
    private IReadOnlyList<DocumentNode> searchMatches = Array.Empty<DocumentNode>();
    private int currentSearchIndex;

    partial void OnErrorMessageChanged(string value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }

    partial void OnInputTextChanged(string value)
    {
        var lineCount = Math.Max(1, value.Count(character => character == '\n') + 1);
        LineNumbersText = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount));
    }

    [RelayCommand]
    private void FormatDocument()
    {
        ClearSearchState();
        rootDocument = null;
        ErrorMessage = string.Empty;
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

        if (searchMatches.Count == 0)
        {
            SearchStatus = "Sin coincidencias";
            return;
        }

        currentSearchIndex = (currentSearchIndex + 1) % searchMatches.Count;
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

        if (searchMatches.Count == 0)
        {
            SearchStatus = "Sin coincidencias";
            return;
        }

        currentSearchIndex = (currentSearchIndex - 1 + searchMatches.Count) % searchMatches.Count;
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
    }

    public void RunSearch()
    {
        if (rootDocument is null)
        {
            SearchStatus = "Primero formatea un documento.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ClearSearchState();
            SearchStatus = "Sin búsqueda";
            return;
        }

        searchMatches = SearchService.FindMatches(rootDocument, SearchText);
        if (searchMatches.Count == 0)
        {
            ClearSearchState();
            SearchStatus = "Sin coincidencias";
            return;
        }

        currentSearchIndex = 0;
        ApplyMatch(currentSearchIndex);
    }

    private void ApplyMatch(int index)
    {
        if (rootDocument is null || searchMatches.Count == 0)
        {
            return;
        }

        ClearSearchState();

        var selected = searchMatches[index];
        selected.IsSearchMatch = true;
        selected.IsCurrentSearchMatch = true;

        var current = selected.Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }

        SelectedNode = selected;
        SelectedNodeDetail = selected.DetailText;
        SearchStatus = $"Coincidencia {index + 1} de {searchMatches.Count}";
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
