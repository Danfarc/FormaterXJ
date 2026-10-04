using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using FormaterXJ.Services;
using FormaterXJ.ViewModels;
using FormaterXJ.Views;

namespace FormaterXJ;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainViewModel();
            try
            {
                var profile = new UserProfileService(UserProfileService.DefaultProfilesDirectory).LoadActiveProfile();
                if (profile is not null)
                {
                    viewModel.ApplyProfile(profile);
                }
            }
            catch (Exception exception) when (exception is IOException
                or InvalidDataException
                or UnauthorizedAccessException
                or System.Text.Json.JsonException)
            {
                viewModel.ErrorMessage = $"No se pudo cargar el perfil: {exception.Message}";
            }

            RequestedThemeVariant = viewModel.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "System" => ThemeVariant.Default,
                _ => ThemeVariant.Dark
            };
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}