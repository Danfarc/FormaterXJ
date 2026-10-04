using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FormaterXJ.Views;

public partial class ProfileDeleteWindow : Window
{
    public ProfileDeleteWindow()
    {
        InitializeComponent();
    }

    public ProfileDeleteWindow(IReadOnlyList<string> profiles)
        : this()
    {
        ProfileSelector.ItemsSource = profiles;
        if (profiles.Count > 0)
        {
            ProfileSelector.SelectedIndex = 0;
        }
    }

    public string? SelectedProfileName => ProfileSelector.SelectedItem as string;

    private void ProfileSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        DeleteButton.IsEnabled = SelectedProfileName is not null;
        ConfirmationText.Text = SelectedProfileName is { } profileName
            ? $"¿Eliminar permanentemente el perfil «{profileName}»?"
            : string.Empty;
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectedProfileName is not null)
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
