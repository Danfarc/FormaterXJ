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
    public void ReturnsNoMatchesWhenNothingMatches()
    {
        var builder = new JsonTreeBuilder();
        var root = builder.Build("{\"name\":\"Ana\"}");

        var matches = SearchService.FindMatches(root, "zzz");

        Assert.Empty(matches);
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
}
