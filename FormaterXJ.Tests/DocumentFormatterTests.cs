using System;
using System.IO;
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
        Assert.Equal(2, result.ErrorLine);
        Assert.True(result.ErrorColumn > 0);
        Assert.InRange(result.ErrorPosition, 0, "{\"name\":\"Ana\"\n\"age\": 42}".Length);
    }

    [Fact]
    public void ReportsXmlErrorLineAndColumn()
    {
        const string xml = "<root>\n  <item>\n</root>";
        var result = DocumentFormatter.Analyze(xml);

        Assert.False(result.IsValid);
        Assert.Equal(3, result.ErrorLine);
        Assert.True(result.ErrorColumn > 0);
        Assert.Equal(xml.LastIndexOf("</root>", StringComparison.Ordinal) + 2, result.ErrorPosition);
    }

    [Fact]
    public void ViewModelMovesErrorCaretToInvalidDocumentLocation()
    {
        var input = "{\n  \"name\": \"Ana\"\n  \"age\": 42\n}";
        var viewModel = new MainViewModel { InputText = input };

        viewModel.FormatDocumentCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.True(viewModel.ErrorPosition >= 0);
        Assert.True(viewModel.ErrorLine > 0);
        Assert.True(viewModel.ErrorColumn > 0);
        Assert.Equal(1, viewModel.ErrorNavigationRequest);
    }

    [Fact]
    public void EditingDocumentValidatesSyntaxWithoutReplacingTextOrMovingCaret()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"name\":\"Ana\"}"
        };
        viewModel.FormatDocumentCommand.Execute(null);
        var errorNavigationRequest = viewModel.ErrorNavigationRequest;
        var invalidInput = "{\n  \"name\": \"Ana\"\n  \"age\": 42\n}";

        viewModel.InputText = invalidInput;

        Assert.True(viewModel.HasError);
        Assert.True(viewModel.ErrorPosition >= 0);
        Assert.True(viewModel.ErrorLine > 0);
        Assert.True(viewModel.ErrorColumn > 0);
        Assert.Equal(errorNavigationRequest, viewModel.ErrorNavigationRequest);
        Assert.Equal(invalidInput, viewModel.InputText);

        viewModel.InputText = "{\"name\":\"Ana\",\"age\":42}";

        Assert.False(viewModel.HasError);
        Assert.Equal(-1, viewModel.ErrorPosition);
        Assert.Equal(errorNavigationRequest, viewModel.ErrorNavigationRequest);
        Assert.Equal($"{{{Environment.NewLine}  \"name\": \"Ana\"{Environment.NewLine}}}", viewModel.FormattedText);
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
    public void BuildsJsonPathForPropertiesWithSpecialCharacters()
    {
        var root = new JsonTreeBuilder().Build("{\"person name\":{\"first.name\":\"Ana\"}}");

        Assert.Equal("$['person name']", root.Children[0].Path);
        Assert.Equal("$['person name']['first.name']", root.Children[0].Children[0].Path);
    }

    [Fact]
    public void BuildsFullXmlPathsAndIndexesRepeatedSiblings()
    {
        var root = new XmlTreeBuilder().Build(
            "<envio id=\"42\"><item>A</item><other/><item>B</item></envio>");

        Assert.Equal("/envio", root.Path);
        Assert.Equal("/envio/@id", root.Children[0].Path);
        Assert.Equal("/envio/item[1]", root.Children[1].Path);
        Assert.Equal("/envio/other", root.Children[2].Path);
        Assert.Equal("/envio/item[2]", root.Children[3].Path);
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
    public void TreeStyleChangesDensityAndCardAppearance()
    {
        var viewModel = new MainViewModel();

        Assert.Equal("Standard", viewModel.TreeStyle);
        Assert.Equal(28, viewModel.TreeItemMinHeight);

        viewModel.TreeStyle = "Compact";
        Assert.Equal(20, viewModel.TreeItemMinHeight);
        Assert.Equal(0, viewModel.TreeHeaderBorderThickness.Left);

        viewModel.TreeStyle = "Cards";
        Assert.Equal(38, viewModel.TreeItemMinHeight);
        Assert.Equal(1, viewModel.TreeHeaderBorderThickness.Left);
        Assert.Equal(8, viewModel.TreeHeaderCornerRadius.TopLeft);
        Assert.Equal(2, viewModel.TreeItemMargin.Top);
    }

    [Fact]
    public void MovingTreeToLeftPlacesDocumentOnRight()
    {
        var viewModel = new MainViewModel();

        Assert.False(viewModel.IsTreeOnLeft);
        Assert.True(viewModel.IsTreeOnRight);
        Assert.Equal(0, viewModel.DocumentPanelColumn);
        Assert.Equal(2, viewModel.TreePanelColumn);

        viewModel.IsTreeOnLeft = true;

        Assert.False(viewModel.IsTreeOnRight);
        Assert.Equal(2, viewModel.DocumentPanelColumn);
        Assert.Equal(0, viewModel.TreePanelColumn);

        viewModel.IsTreeOnRight = true;

        Assert.False(viewModel.IsTreeOnLeft);
        Assert.Equal(0, viewModel.DocumentPanelColumn);
        Assert.Equal(2, viewModel.TreePanelColumn);
    }

    [Fact]
    public void ExpandsAndCollapsesAllNodesInTree()
    {
        var viewModel = new MainViewModel
        {
            InputText = "{\"person\":{\"name\":\"Ana\"},\"items\":[1,2]}"
        };
        viewModel.FormatDocumentCommand.Execute(null);
        var nodes = viewModel.RootNodes[0]
            .Children
            .SelectMany(node => new[] { node }.Concat(node.Children))
            .ToList();

        viewModel.CollapseAllNodesCommand.Execute(null);

        Assert.All(nodes, node => Assert.False(node.IsExpanded));

        viewModel.ExpandAllNodesCommand.Execute(null);

        Assert.All(nodes, node => Assert.True(node.IsExpanded));
    }

    [Fact]
    public void SavesAndLoadsUserProfileSettings()
    {
        var profilesDirectory = Path.Combine(Path.GetTempPath(), $"FormaterXJ-{Guid.NewGuid():N}");
        var service = new UserProfileService(profilesDirectory);
        var viewModel = new MainViewModel
        {
            EditorFontSize = 14,
            TreeFontSize = 18,
            ShowLineNumbers = false,
            IsTreeOnLeft = true,
            Theme = "Light",
            TreeStyle = "Cards"
        };

        try
        {
            service.Save("Trabajo", viewModel.CreateProfile());
            var loadedProfile = service.Load("Trabajo");

            Assert.NotNull(loadedProfile);
            var restoredViewModel = new MainViewModel();
            restoredViewModel.ApplyProfile(loadedProfile);

            Assert.Equal(14, restoredViewModel.EditorFontSize);
            Assert.Equal(18, restoredViewModel.TreeFontSize);
            Assert.False(restoredViewModel.ShowLineNumbers);
            Assert.True(restoredViewModel.IsTreeOnLeft);
            Assert.Equal("Light", restoredViewModel.Theme);
            Assert.Equal("Cards", restoredViewModel.TreeStyle);
            Assert.Equal("Trabajo", loadedProfile.Name);
            Assert.Equal("Trabajo", service.LoadActiveProfile()?.Name);
            Assert.Equal(new[] { "Trabajo" }, service.ListProfiles());
        }
        finally
        {
            if (Directory.Exists(profilesDirectory))
            {
                Directory.Delete(profilesDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void EditingProfileDoesNotActivateItUntilSavedAndDeletingRemovesIt()
    {
        var profilesDirectory = Path.Combine(Path.GetTempPath(), $"FormaterXJ-{Guid.NewGuid():N}");
        var service = new UserProfileService(profilesDirectory);

        try
        {
            service.Save("Trabajo", new UserProfile { Theme = "Light" });
            service.Save("Personal", new UserProfile { Theme = "Dark" });

            var editedProfile = service.Read("Trabajo");
            editedProfile.Theme = "System";
            Assert.Equal("Personal", service.LoadActiveProfile()?.Name);

            service.Save(editedProfile.Name, editedProfile);
            Assert.Equal("System", service.Load("Trabajo").Theme);

            service.Delete("Trabajo");
            Assert.DoesNotContain("Trabajo", service.ListProfiles());
            Assert.False(File.Exists(Path.Combine(profilesDirectory, "Trabajo.json")));
        }
        finally
        {
            if (Directory.Exists(profilesDirectory))
            {
                Directory.Delete(profilesDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void RecentFilesAreDeduplicatedAndLimitedToTen()
    {
        var recentFilesPath = Path.Combine(Path.GetTempPath(), $"FormaterXJ-{Guid.NewGuid():N}.json");
        var service = new RecentFilesService(recentFilesPath);

        try
        {
            for (var index = 0; index < RecentFilesService.MaximumRecentFiles + 2; index++)
            {
                service.Add(Path.Combine(Path.GetTempPath(), $"document-{index}.json"));
            }

            service.Add(Path.Combine(Path.GetTempPath(), "document-5.json"));

            var recentFiles = service.Load();
            Assert.Equal(RecentFilesService.MaximumRecentFiles, recentFiles.Count);
            Assert.Equal(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "document-5.json")), recentFiles[0]);
            Assert.DoesNotContain(recentFiles, path => path.EndsWith("document-0.json", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(recentFilesPath))
            {
                File.Delete(recentFilesPath);
            }
        }
    }

    [Theory]
    [InlineData("document.json", true)]
    [InlineData("document.XML", true)]
    [InlineData("document.txt", true)]
    [InlineData("document.exe", false)]
    [InlineData("", false)]
    public void ChecksSupportedRecentFileTypes(string path, bool isSupported)
    {
        Assert.Equal(isSupported, RecentFilesService.IsSupportedFile(path));
    }

    [Fact]
    public void SavesAndSelectsMultipleNamedProfiles()
    {
        var profilesDirectory = Path.Combine(Path.GetTempPath(), $"FormaterXJ-{Guid.NewGuid():N}");
        var service = new UserProfileService(profilesDirectory);

        try
        {
            service.Save("Trabajo", new UserProfile { Theme = "Light" });
            service.Save("Personal", new UserProfile { Theme = "System" });

            Assert.Equal(new[] { "Personal", "Trabajo" }, service.ListProfiles());
            Assert.Equal("System", service.Load("Personal").Theme);
            Assert.Equal("Personal", service.LoadActiveProfile()?.Name);
            Assert.Equal("Light", service.Load("Trabajo").Theme);
            Assert.Equal("Trabajo", service.LoadActiveProfile()?.Name);
        }
        finally
        {
            if (Directory.Exists(profilesDirectory))
            {
                Directory.Delete(profilesDirectory, recursive: true);
            }
        }
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
