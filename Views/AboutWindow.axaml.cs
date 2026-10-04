using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FormaterXJ.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = typeof(App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(App).Assembly.GetName().Version?.ToString(3)
            ?? "1.0.0";
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
