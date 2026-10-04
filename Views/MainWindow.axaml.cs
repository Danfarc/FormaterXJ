using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FormaterXJ.ViewModels;

namespace FormaterXJ.Views;

public partial class MainWindow : Window
{
    private ScrollViewer? editorScrollViewer;
    private ScrollViewer? lineNumberScrollViewer;

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

        if (editorScrollViewer is not null)
        {
            editorScrollViewer.ScrollChanged += EditorScrollViewer_ScrollChanged;
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
