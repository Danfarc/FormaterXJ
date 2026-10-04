using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FormaterXJ.Models;
using FormaterXJ.Services;
using FormaterXJ.ViewModels;

namespace FormaterXJ.Views;

public partial class MainWindow : Window
{
    private ScrollViewer? editorScrollViewer;
    private ScrollViewer? lineNumberScrollViewer;
    private MainViewModel? viewModel;
    private SearchWindow? searchWindow;
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

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2 || e.Source != sender)
        {
            return;
        }

        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        e.Handled = true;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ErrorNavigationRequest)
            && viewModel is { ErrorPosition: >= 0 } errorViewModel)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var errorPosition = Math.Clamp(errorViewModel.ErrorPosition, 0, DocumentEditor.Text?.Length ?? 0);
                DocumentEditor.CaretIndex = errorPosition;
                DocumentEditor.SelectionStart = errorPosition;
                DocumentEditor.SelectionEnd = errorPosition;
                DocumentEditor.Focus();
            });
            return;
        }

        if (e.PropertyName != nameof(MainViewModel.SelectedNode)
            && e.PropertyName != nameof(MainViewModel.SearchSelectionStart))
        {
            return;
        }

        var currentViewModel = viewModel;
        if (currentViewModel is null
            || currentViewModel.SelectedNode is not { EditorStart: >= 0 } node
            || synchronizingFromEditor
            || currentViewModel.InputText != currentViewModel.FormattedText)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (currentViewModel.SelectedNode != node)
            {
                return;
            }

            BringNodeIntoView(node);
            synchronizingFromEditor = true;
            try
            {
                var searchSelectionStart = currentViewModel.SearchSelectionStart;
                var selectionStart = searchSelectionStart >= 0
                    ? searchSelectionStart
                    : node.EditorBlockStart >= 0
                        ? node.EditorBlockStart
                        : node.EditorStart;
                var selectionLength = searchSelectionStart >= 0
                    ? currentViewModel.SearchSelectionLength
                    : node.EditorBlockStart >= 0
                        ? node.EditorBlockLength
                        : node.EditorLength;
                DocumentEditor.CaretIndex = selectionStart;
                DocumentEditor.SelectionStart = selectionStart;
                DocumentEditor.SelectionEnd = selectionStart + selectionLength;
                if (searchWindow is not { IsActive: true, IsKeyboardFocusWithin: true })
                {
                    DocumentEditor.Focus();
                }
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

    private void SearchMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel currentViewModel)
        {
            return;
        }

        if (searchWindow is { IsVisible: true })
        {
            searchWindow.Activate();
            searchWindow.FocusSearchInput();
            return;
        }

        searchWindow = new SearchWindow
        {
            DataContext = currentViewModel
        };
        searchWindow.Closed += (_, _) => searchWindow = null;
        searchWindow.Show(this);
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

    private void NodeContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is ContextMenu contextMenu
            && contextMenu.PlacementTarget?.DataContext is DocumentNode node
            && DataContext is MainViewModel currentViewModel)
        {
            contextMenu.DataContext = node;
            currentViewModel.SelectedNode = node;
            currentViewModel.SelectedNodeDetail = node.DetailText;
        }
    }

    private async void CopyNodePath_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem
            || menuItem.Parent is not ContextMenu { DataContext: DocumentNode node }
            || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        try
        {
            await clipboard.SetTextAsync(node.Path);
            if (DataContext is MainViewModel currentViewModel)
            {
                currentViewModel.ProfileStatus = $"Ruta copiada: {node.Path}";
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or System.Runtime.InteropServices.COMException)
        {
            if (DataContext is MainViewModel currentViewModel)
            {
                currentViewModel.ErrorMessage = $"No se pudo copiar la ruta: {exception.Message}";
            }
        }
    }

    private void DarkTheme_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Theme = "Dark";
        }

        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
    }

    private void LightTheme_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Theme = "Light";
        }

        Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
    }

    private void SystemTheme_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Theme = "System";
        }

        Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    }

    private async void SaveProfileMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel currentViewModel)
        {
            return;
        }

        var nameDialog = new ProfileNameWindow();
        var shouldSave = await nameDialog.ShowDialog<bool>(this);
        if (!shouldSave)
        {
            return;
        }

        try
        {
            var profileService = new UserProfileService(UserProfileService.DefaultProfilesDirectory);
            profileService.Save(nameDialog.ProfileName, currentViewModel.CreateProfile());
            currentViewModel.ProfileStatus = $"Perfil «{nameDialog.ProfileName}» guardado y activo.";
            ApplyTheme(currentViewModel.Theme);
        }
        catch (Exception exception) when (exception is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            currentViewModel.ProfileStatus = string.Empty;
            currentViewModel.ErrorMessage = $"No se pudo guardar el perfil: {exception.Message}";
        }
    }

    private async void SelectProfileMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel currentViewModel)
        {
            return;
        }

        try
        {
            var profileService = new UserProfileService(UserProfileService.DefaultProfilesDirectory);
            var profiles = profileService.ListProfiles();
            if (profiles.Count == 0)
            {
                currentViewModel.ProfileStatus = "No hay perfiles guardados.";
                return;
            }

            var picker = new ProfilePickerWindow(profiles);
            if (await picker.ShowDialog<bool>(this) && picker.SelectedProfileName is { } selectedName)
            {
                var profile = profileService.Load(selectedName);
                currentViewModel.ApplyProfile(profile);
                currentViewModel.ProfileStatus = $"Perfil «{selectedName}» cargado.";
                ApplyTheme(profile.Theme);
            }
        }
        catch (Exception exception) when (exception is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or System.Text.Json.JsonException)
        {
            currentViewModel.ProfileStatus = string.Empty;
            currentViewModel.ErrorMessage = $"No se pudo cargar el perfil: {exception.Message}";
        }
    }

    private static void ApplyTheme(string theme)
    {
        Application.Current!.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "System" => ThemeVariant.Default,
            _ => ThemeVariant.Dark
        };
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

    private void FileMenu_SubmenuOpened(object? sender, RoutedEventArgs e)
    {
        RefreshRecentFilesMenu();
    }

    private void RefreshRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();
        IReadOnlyList<string> recentFiles;
        try
        {
            recentFiles = new RecentFilesService(RecentFilesService.DefaultRecentFilesPath).Load();
        }
        catch (Exception exception) when (exception is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or System.Text.Json.JsonException)
        {
            if (DataContext is MainViewModel currentViewModel)
            {
                currentViewModel.ErrorMessage = $"No se pudo leer la lista de archivos recientes: {exception.Message}";
            }

            return;
        }

        if (recentFiles.Count == 0)
        {
            RecentFilesMenu.Items.Add(new MenuItem
            {
                Header = "(Sin archivos recientes)",
                IsEnabled = false
            });
            return;
        }

        foreach (var path in recentFiles)
        {
            var menuItem = new MenuItem
            {
                Header = Path.GetFileName(path),
                Tag = path
            };
            ToolTip.SetTip(menuItem, path);
            menuItem.Click += RecentFile_Click;
            RecentFilesMenu.Items.Add(menuItem);
        }
    }

    private async void RecentFile_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string path })
        {
            await LoadFileAsync(path);
        }
    }

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        e.DragEffects = files?.OfType<IStorageFile>()
            .Any(file => RecentFilesService.IsSupportedFile(file.Path.LocalPath)) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object? sender, DragEventArgs e)
    {
        var file = e.DataTransfer.TryGetFiles()?.OfType<IStorageFile>()
            .FirstOrDefault(item => RecentFilesService.IsSupportedFile(item.Path.LocalPath));
        if (file is null)
        {
            return;
        }

        e.Handled = true;
        await LoadFileAsync(file.Path.LocalPath);
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

        await LoadFileAsync(file.Path.LocalPath);
    }

    private async Task LoadFileAsync(string path)
    {
        if (!RecentFilesService.IsSupportedFile(path))
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ErrorMessage = "El archivo debe tener extensión JSON, XML o TXT.";
            }

            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(path);
            if (DataContext is MainViewModel currentViewModel)
            {
                currentViewModel.LoadText(content);
                new RecentFilesService(RecentFilesService.DefaultRecentFilesPath).Add(path);
                RefreshRecentFilesMenu();
            }
        }
        catch (Exception exception) when (exception is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or System.Text.Json.JsonException)
        {
            if (DataContext is MainViewModel currentViewModel)
            {
                currentViewModel.ErrorMessage = $"No se pudo abrir el archivo: {exception.Message}";
            }
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
