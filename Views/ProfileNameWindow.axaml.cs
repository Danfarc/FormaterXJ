using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FormaterXJ.Services;

namespace FormaterXJ.Views;

public partial class ProfileNameWindow : Window
{
    public ProfileNameWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => NameInput.Focus());
    }

    public string ProfileName { get; private set; } = string.Empty;

    private void NameInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Save_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var name = NameInput.Text?.Trim();
        if (!UserProfileService.IsValidProfileName(name))
        {
            NameInput.Focus();
            return;
        }

        ProfileName = name!;
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
