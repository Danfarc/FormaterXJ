using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FormaterXJ.Views;

public partial class ProfilePickerWindow : Window
{
    public ProfilePickerWindow()
    {
        InitializeComponent();
    }

    public ProfilePickerWindow(IReadOnlyList<string> profiles)
        : this()
    {
        ProfilesList.ItemsSource = profiles;
        if (profiles.Count > 0)
        {
            ProfilesList.SelectedIndex = 0;
        }
    }

    public string? SelectedProfileName => ProfilesList.SelectedItem as string;

    private void ProfilesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadButton.IsEnabled = ProfilesList.SelectedItem is string;
    }

    private void Load_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectedProfileName is not null)
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
