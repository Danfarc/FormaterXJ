using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FormaterXJ.Models;

namespace FormaterXJ.Views;

public partial class ProfileEditorWindow : Window
{
    private bool initializing;

    public ProfileEditorWindow()
    {
        InitializeComponent();
        ProfileSelector.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(UserProfile.Name));
        ThemeSelector.ItemsSource = new[] { "Dark", "Light", "System" };
        TreePositionSelector.ItemsSource = new[] { "Derecha", "Izquierda" };
        TreeStyleSelector.ItemsSource = new[] { "Standard", "Compact", "Cards" };
        EditorFontSelector.ItemsSource = new[] { 10d, 12d, 14d, 16d, 18d };
        TreeFontSelector.ItemsSource = new[] { 10d, 12d, 13d, 14d, 16d, 18d };
    }

    public ProfileEditorWindow(IReadOnlyList<UserProfile> profiles)
        : this()
    {
        ProfileSelector.ItemsSource = profiles;

        if (profiles.Count > 0)
        {
            ProfileSelector.SelectedIndex = 0;
        }
    }

    public UserProfile? EditedProfile { get; private set; }

    private void ProfileSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ProfileSelector.SelectedItem is not UserProfile profile)
        {
            return;
        }

        initializing = true;
        ThemeSelector.SelectedItem = profile.Theme;
        TreePositionSelector.SelectedItem = profile.IsTreeOnLeft ? "Izquierda" : "Derecha";
        TreeStyleSelector.SelectedItem = profile.TreeStyle;
        EditorFontSelector.SelectedItem = profile.EditorFontSize;
        TreeFontSelector.SelectedItem = profile.TreeFontSize;
        LineNumbersOption.IsChecked = profile.ShowLineNumbers;
        initializing = false;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (initializing || ProfileSelector.SelectedItem is not UserProfile profile)
        {
            return;
        }

        EditedProfile = new UserProfile
        {
            Name = profile.Name,
            Theme = ThemeSelector.SelectedItem as string ?? profile.Theme,
            IsTreeOnLeft = Equals(TreePositionSelector.SelectedItem, "Izquierda"),
            TreeStyle = TreeStyleSelector.SelectedItem as string ?? profile.TreeStyle,
            EditorFontSize = EditorFontSelector.SelectedItem is double editorSize
                ? editorSize
                : profile.EditorFontSize,
            TreeFontSize = TreeFontSelector.SelectedItem is double treeSize
                ? treeSize
                : profile.TreeFontSize,
            ShowLineNumbers = LineNumbersOption.IsChecked == true
        };
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
