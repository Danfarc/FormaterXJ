using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FormaterXJ.Models;
using FormaterXJ.ViewModels;

namespace FormaterXJ.Views;

public partial class MainWindow : Window
{
    private ScrollViewer? editorScrollViewer;
    private ScrollViewer? lineNumberScrollViewer;
    private MainViewModel? viewModel;
    private bool synchronizingFromEditor;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        editorScrollViewer = DocumentEditor.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
        lineNumberScrollViewer = LineNumberScrollViewer;
        viewModel = DataContext as MainViewModel;

        if (editorScrollViewer is not null)
        {
            editorScrollViewer.ScrollChanged += EditorScrollViewer_ScrollChanged;
        }

        DocumentEditor.PropertyChanged += DocumentEditor_PropertyChanged;
        if (viewModel is not null)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void DocumentEditor_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (synchronizingFromEditor
            || (e.Property != TextBox.CaretIndexProperty
                && e.Property != TextBox.SelectionStartProperty
                && e.Property != TextBox.SelectionEndProperty))
        {
            return;
        }

        if (viewModel is null || viewModel.InputText != viewModel.FormattedText)
        {
            return;
        }

        synchronizingFromEditor = true;
        try
        {
            var position = DocumentEditor.SelectionStart != DocumentEditor.SelectionEnd
                ? DocumentEditor.SelectionStart
                : DocumentEditor.CaretIndex;
            viewModel.SelectNodeAtEditorPosition(position);
            if (viewModel.SelectedNode is not null)
            {
                BringNodeIntoView(viewModel.SelectedNode);
            }
        }
        finally
        {
            synchronizingFromEditor = false;
        }
    }

    private void DocumentEditor_PastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => viewModel?.FormatDocumentCommand.Execute(null));
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.SelectedNode)
            || synchronizingFromEditor
            || viewModel?.InputText != viewModel?.FormattedText
            || viewModel?.SelectedNode is not { EditorStart: >= 0 } node)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (viewModel.SelectedNode != node)
            {
                return;
            }

            BringNodeIntoView(node);
            synchronizingFromEditor = true;
            try
            {
                var selectionStart = node.EditorBlockStart >= 0 ? node.EditorBlockStart : node.EditorStart;
                var selectionLength = node.EditorBlockStart >= 0 ? node.EditorBlockLength : node.EditorLength;
                DocumentEditor.CaretIndex = selectionStart;
                DocumentEditor.SelectionStart = selectionStart;
                DocumentEditor.SelectionEnd = selectionStart + selectionLength;
                DocumentEditor.Focus();
            }
            finally
            {
                synchronizingFromEditor = false;
            }
        });
    }

    private void BringNodeIntoView(DocumentNode node)
    {
        var ancestors = new System.Collections.Generic.Stack<DocumentNode>();
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            ancestors.Push(parent);
        }

        ItemsControl containerOwner = StructureTree;
        while (ancestors.Count > 0)
        {
            if (containerOwner.ContainerFromItem(ancestors.Pop()) is TreeViewItem parentContainer)
            {
                parentContainer.IsExpanded = true;
                parentContainer.UpdateLayout();
                containerOwner = parentContainer;
            }
            else
            {
                return;
            }
        }

        if (containerOwner.ContainerFromItem(node) is TreeViewItem nodeContainer)
        {
            nodeContainer.BringIntoView();
        }
    }

    private void EditorScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (editorScrollViewer is not null && lineNumberScrollViewer is not null)
        {
            lineNumberScrollViewer.Offset = new Avalonia.Vector(
                lineNumberScrollViewer.Offset.X,
                editorScrollViewer.Offset.Y);
        }
    }

    private async void OpenMenuItem_Click(object sender, RoutedEventArgs e) => await OpenFileAsync();

    private void EditorFontSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string value }
            && double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var fontSize)
            && DataContext is MainViewModel viewModel)
        {
            viewModel.EditorFontSize = fontSize;
        }
    }

    private void TreeFontSize_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string value }
            && double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var fontSize)
            && DataContext is MainViewModel viewModel)
        {
            viewModel.TreeFontSize = fontSize;
        }
    }

    private void LineNumbersMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && DataContext is MainViewModel viewModel)
        {
            viewModel.ShowLineNumbers = menuItem.IsChecked;
        }
    }

    private void DarkTheme_Click(object sender, RoutedEventArgs e)
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
    }

    private void LightTheme_Click(object sender, RoutedEventArgs e)
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    }

    private void SystemTheme_Click(object sender, RoutedEventArgs e)
    {
        Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    }

    private async void SaveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.FormattedText))
        {
            viewModel.ErrorMessage = "Primero formatea un documento JSON o XML válido.";
            return;
        }

        var defaultExtension = viewModel.DetectedFormat == "JSON" ? "json" : "xml";
        var file = await SaveFileAsync(defaultExtension);
        if (file is null)
        {
            return;
        }

        await File.WriteAllTextAsync(file.Path.LocalPath, viewModel.FormattedText);
    }

    private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task OpenFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona un archivo JSON o XML",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON/XML")
                {
                    Patterns = ["*.json", "*.xml", "*.txt"]
                }
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var content = await File.ReadAllTextAsync(file.Path.LocalPath);
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.LoadText(content);
        }
    }

    private async Task<IStorageFile?> SaveFileAsync(string defaultExtension)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar documento formateado",
            SuggestedFileName = $"documento.{defaultExtension}",
            DefaultExtension = defaultExtension,
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType("Documentos")
                {
                    Patterns = ["*.json", "*.xml", "*.txt"]
                }
            ]
        });

        return file;
    }
}
