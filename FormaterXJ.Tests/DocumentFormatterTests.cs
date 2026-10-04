using FormaterXJ.Models;
using FormaterXJ.Services;
using FormaterXJ.ViewModels;
using Xunit;

namespace FormaterXJ.Tests;

public class DocumentFormatterTests
{
    [Fact]
    public void DetectsJsonAndFormatsWithTwoSpaceIndentation()
    {
        var result = DocumentFormatter.Analyze("{\"name\":\"Ana\",\"age\":30,\"roles\":[\"admin\",\"user\"]}");

        Assert.True(result.IsValid);
        Assert.Equal("JSON", result.DetectLabel);
        Assert.Contains("\"name\": \"Ana\"", result.FormattedText);
        Assert.Contains("\"roles\": [", result.FormattedText);
    }

    [Fact]
    public void DetectsXmlAndPreservesAttributes()
    {
        var result = DocumentFormatter.Analyze("<user id=\"42\"><name>Ana</name></user>");

        Assert.True(result.IsValid);
        Assert.Equal("XML", result.DetectLabel);
        Assert.Contains("<user", result.FormattedText);
        Assert.Contains("id=\"42\"", result.FormattedText);
        Assert.Contains("<name>Ana</name>", result.FormattedText);
    }

    [Fact]
    public void FormatsXmlOutputAgainWithoutError()
    {
        var firstResult = DocumentFormatter.Analyze("<user id=\"42\"><name>Ana</name></user>");

        Assert.True(firstResult.IsValid);
        Assert.False(firstResult.FormattedText.StartsWith('\uFEFF'));

        var secondResult = DocumentFormatter.Analyze(firstResult.FormattedText);

        Assert.True(secondResult.IsValid, secondResult.ErrorMessage);
        Assert.Equal(DocumentFormatKind.Xml, secondResult.Kind);
    }

    [Fact]
    public void FormatsXmlTwiceThroughTheViewModel()
    {
        var viewModel = new MainViewModel
        {
            InputText = "<user id=\"42\"><name>Ana</name></user>"
        };

        viewModel.FormatDocumentCommand.Execute(null);
        Assert.Empty(viewModel.ErrorMessage);

        viewModel.FormatDocumentCommand.Execute(null);

        Assert.Empty(viewModel.ErrorMessage);
        Assert.Equal("XML", viewModel.DetectedFormat);
        Assert.True(viewModel.HasValidDocument);
    }

    [Fact]
    public void LocatesJsonPropertyAndXmlAttributeInFormattedText()
    {
        const string json = "{\n  \"name\": \"Ana\"\n}";
        var jsonRoot = new JsonTreeBuilder().Build(json);
        DocumentTextLocator.Locate(jsonRoot, json, DocumentFormatKind.Json);

        var jsonProperty = jsonRoot.Children[0];
        Assert.Equal("\"name\"", json.Substring(jsonProperty.EditorStart, jsonProperty.EditorLength));

        const string xml = "<user id=\"42\"><name>Ana</name></user>";
        var xmlRoot = new XmlTreeBuilder().Build(xml);
        DocumentTextLocator.Locate(xmlRoot, xml, DocumentFormatKind.Xml);

        var xmlAttribute = xmlRoot.Children[0];
        Assert.Equal("id", xml.Substring(xmlAttribute.EditorStart, xmlAttribute.EditorLength));
    }

    [Fact]
    public void LocatesCompleteJsonObjectAndPropertyBlocks()
    {
        const string json = "{\n  \"person\": {\n    \"name\": \"Ana\"\n  }\n}";
        var root = new JsonTreeBuilder().Build(json);
        DocumentTextLocator.Locate(root, json, DocumentFormatKind.Json);

        var person = root.Children[0];
        var personBlock = json.Substring(person.EditorBlockStart, person.EditorBlockLength);
        Assert.Contains("\"person\"", personBlock);
        Assert.Contains("\"name\": \"Ana\"", personBlock);
        Assert.EndsWith("}", personBlock);

        var name = person.Children[0];
        var nameBlock = json.Substring(name.EditorBlockStart, name.EditorBlockLength);
        Assert.Equal("\"name\": \"Ana\"", nameBlock.Trim());
    }

    [Fact]
    public void LocatesCompleteXmlElementAndAttributeBlocks()
    {
        const string xml = "<user id=\"42\"><name>Ana</name></user>";
        var root = new XmlTreeBuilder().Build(xml);
        DocumentTextLocator.Locate(root, xml, DocumentFormatKind.Xml);

        var userBlock = xml.Substring(root.EditorBlockStart, root.EditorBlockLength);
        Assert.Equal(xml, userBlock);

        var attribute = root.Children[0];
        var attributeBlock = xml.Substring(attribute.EditorBlockStart, attribute.EditorBlockLength);
        Assert.Equal("id=\"42\"", attributeBlock);
    }

    [Fact]
    public void EditorPositionSelectsMatchingTreeNode()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"Ana\"}"
        };
        viewModel.FormatDocumentCommand.Execute(null);

        var propertyPosition = viewModel.InputText.IndexOf("\"name\"", StringComparison.Ordinal);
        viewModel.SelectNodeAtEditorPosition(propertyPosition);

        Assert.Equal("name", viewModel.SelectedNode?.Name);
    }

    [Fact]
    public void EditorPositionOutsideTextBoundsDoesNotThrow()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"Ana\"}"
        };
        viewModel.FormatDocumentCommand.Execute(null);

        viewModel.SelectNodeAtEditorPosition(int.MaxValue);

        Assert.NotNull(viewModel.SelectedNode);
    }

    [Fact]
    public void EmptyEditorTextDoesNotThrowDuringSelectionSynchronization()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"Ana\"}"
        };
        viewModel.FormatDocumentCommand.Execute(null);
        viewModel.InputText = string.Empty;

        viewModel.SelectNodeAtEditorPosition(0);

        Assert.Equal("Documento", viewModel.SelectedNode?.Name);
    }

    [Fact]
    public void RejectsMalformedInput()
    {
        var result = DocumentFormatter.Analyze("{\"name\":\"Ana\"\n\"age\": 42}");

        Assert.False(result.IsValid);
        Assert.Contains("JSON", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsEmptyInput()
    {
        var result = DocumentFormatter.Analyze("   \r\n\t ");

        Assert.False(result.IsValid);
        Assert.Contains("vacío", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildsJsonTreeFromArrayAndObjectNodes()
    {
        var builder = new JsonTreeBuilder();
        var root = builder.Build("{\"person\":{\"name\":\"Ana\"},\"items\":[1,2,3]}");

        Assert.Equal("Documento", root.Name);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal("person", root.Children[0].Name);
        Assert.Equal("items", root.Children[1].Name);
        Assert.Equal("name", root.Children[0].Children[0].Name);
        Assert.Equal("Ana", root.Children[0].Children[0].Value);
    }

    [Fact]
    public void ShowsXmlElementValueInTreeWhenElementHasAttributes()
    {
        var root = new XmlTreeBuilder().Build(
            "<envio><numeroSeguimiento tipo=\"guia\">DHL-998231-XYZ</numeroSeguimiento></envio>");
        var trackingNumber = root.Children[0];

        Assert.Equal("DHL-998231-XYZ", trackingNumber.Value);
        Assert.Equal("numeroSeguimiento: DHL-998231-XYZ (1)", trackingNumber.DisplayText);
    }

    [Fact]
    public void FindsSearchMatchesCaseInsensitiveAcrossAttributesAndValues()
    {
        var builder = new XmlTreeBuilder();
        var root = builder.Build("<user id=\"42\" role=\"admin\"><name>Ana</name></user>");

        var matches = SearchService.FindMatches(root, "ADMIN");

        Assert.Single(matches);
        Assert.Equal("@role", matches[0].Name);
        Assert.Equal("admin", matches[0].Value);
    }

    [Fact]
    public void SearchCanMatchCaseAndWholeWords()
    {
        var root = new JsonTreeBuilder().Build(
            "{\"exact\":\"Ana\",\"embedded\":\"anagram\",\"capitalized\":\"ANAGRAM\",\"underscored\":\"_ana\"}");

        var caseSensitiveMatches = SearchService.FindMatches(root, "Ana", matchCase: true);
        var wholeWordMatches = SearchService.FindMatches(root, "ana", matchWholeWord: true);

        Assert.Single(caseSensitiveMatches);
        Assert.Single(wholeWordMatches);
        Assert.Equal("exact", caseSensitiveMatches[0].Name);
        Assert.Equal("exact", wholeWordMatches[0].Name);
    }

    [Fact]
    public void SearchOptionsDefaultToNormalCaseInsensitivePartialSearch()
    {
        var viewModel = new MainViewModel();
        var root = new JsonTreeBuilder().Build("{\"name\":\"ANAGRAM\"}");

        Assert.False(viewModel.SearchMatchCase);
        Assert.False(viewModel.SearchWholeWord);
        Assert.Single(SearchService.FindMatches(root, "ana"));
    }

    [Fact]
    public void DocumentSearchNavigatesAndSelectsEachExactOccurrence()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"Ana Ana\"}"
        };
        viewModel.FormatDocumentCommand.Execute(null);
        viewModel.SearchText = "Ana";
        var firstMatch = viewModel.FormattedText.IndexOf("Ana", StringComparison.Ordinal);

        Assert.Equal(3, viewModel.SearchSelectionLength);
        Assert.Equal(firstMatch, viewModel.SearchSelectionStart);
        Assert.Equal("Coincidencia 1 de 2", viewModel.SearchStatus);

        viewModel.SearchNextCommand.Execute(null);

        var secondMatch = viewModel.FormattedText.IndexOf("Ana", firstMatch + 1, StringComparison.Ordinal);
        Assert.Equal(secondMatch, viewModel.SearchSelectionStart);
        Assert.Equal("Coincidencia 2 de 2", viewModel.SearchStatus);

        viewModel.SearchPreviousCommand.Execute(null);

        Assert.Equal(firstMatch, viewModel.SearchSelectionStart);
        Assert.Equal("Coincidencia 1 de 2", viewModel.SearchStatus);
    }

    [Fact]
    public void ReturnsNoMatchesWhenNothingMatches()
    {
        var builder = new JsonTreeBuilder();
        var root = builder.Build("{\"name\":\"Ana\"}");

        var matches = SearchService.FindMatches(root, "zzz");

        Assert.Empty(matches);
    }

    [Fact]
    public void UpdatesSearchMatchesAndStatusAsQueryChanges()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"Ana\",\"role\":\"admin\"}"
        };
        viewModel.FormatDocumentCommand.Execute(null);

        viewModel.SearchText = "ana";

        Assert.Equal("Coincidencia 1 de 1", viewModel.SearchStatus);
        Assert.Equal("name", viewModel.SelectedNode?.Name);
        Assert.True(viewModel.SelectedNode?.IsCurrentSearchMatch);

        viewModel.SearchText = "no existe";

        Assert.Equal("Sin coincidencias", viewModel.SearchStatus);
        Assert.False(viewModel.RootNodes[0].Children[0].IsSearchMatch);
        Assert.False(viewModel.RootNodes[0].Children[0].IsCurrentSearchMatch);
    }

    [Fact]
    public void ChangingSearchOptionsImmediatelyReappliesSearch()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"ANAGRAM\"}",
            SearchText = "ana"
        };
        viewModel.FormatDocumentCommand.Execute(null);
        Assert.Equal("Coincidencia 1 de 1", viewModel.SearchStatus);

        viewModel.SearchMatchCase = true;
        Assert.Equal("Sin coincidencias", viewModel.SearchStatus);

        viewModel.SearchMatchCase = false;
        viewModel.SearchWholeWord = true;
        Assert.Equal("Sin coincidencias", viewModel.SearchStatus);
    }

    [Fact]
    public void NotifiesWhenSearchHighlightChanges()
    {
        var node = new DocumentNode();
        var searchBackgroundChanged = false;
        node.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DocumentNode.SearchBackground))
            {
                searchBackgroundChanged = true;
            }
        };

        node.IsSearchMatch = true;

        Assert.True(searchBackgroundChanged);
    }

    [Fact]
    public void UpdatesLineNumberGutterForEditorText()
    {
        var viewModel = new MainViewModel();

        Assert.True(viewModel.ShowLineNumbers);

        viewModel.InputText = "line one\r\nline two\nline three";
        Assert.Equal($"1{Environment.NewLine}2{Environment.NewLine}3", viewModel.LineNumbersText);
    }

    [Fact]
    public void UsesTenPointEditorFontByDefault()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(10, viewModel.EditorFontSize);
    }

    [Fact]
    public void ScalesTreeExpanderAndSpacingWithTreeFontSize()
    {
        var viewModel = new MainViewModel
        {
            TreeFontSize = 18
        };

        Assert.Equal(18 * 12d / 13, viewModel.TreeExpanderSize, 10);
        Assert.Equal(18 * 28d / 13, viewModel.TreeItemMinHeight, 10);
        Assert.Equal(18 * 4d / 13, viewModel.TreeItemPadding.Left, 10);
        Assert.Equal(18 * 12d / 13, viewModel.TreeExpanderMargin.Left, 10);
    }

    [Fact]
    public void UpdatesLineNumberGutterWhenFormattingDocument()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"Ana\"}"
        };

        viewModel.FormatDocumentCommand.Execute(null);

        Assert.True(viewModel.ShowLineNumbers);
        Assert.Equal(3, viewModel.LineNumbersText.Split(Environment.NewLine).Length);
    }

    [Fact]
    public void LoadingTextFormatsItImmediately()
    {
        var viewModel = new MainViewModel();

        viewModel.LoadText("{\"name\":\"Ana\"}");

        Assert.True(viewModel.HasValidDocument);
        Assert.Equal($"{{{Environment.NewLine}  \"name\": \"Ana\"{Environment.NewLine}}}", viewModel.InputText);
    }
}
