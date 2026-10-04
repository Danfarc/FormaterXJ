using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FormaterXJ.ViewModels;

namespace FormaterXJ.Views;

public partial class SearchWindow : Window
{
    public SearchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => FocusSearchInput();
    }

    public void FocusSearchInput()
    {
        Dispatcher.UIThread.Post(() =>
        {
            SearchInput.Focus();
            SearchInput.CaretIndex = SearchInput.Text?.Length ?? 0;
        });
    }

    private void SearchInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || e.Key != Key.Enter)
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            viewModel.SearchPreviousCommand.Execute(null);
        }
        else
        {
            viewModel.SearchNextCommand.Execute(null);
        }

        e.Handled = true;
    }
}
