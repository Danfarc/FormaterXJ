# Copilot instructions for FormaterXJ

## Project overview
This repository is a small Avalonia desktop application targeting .NET 10. The app is intentionally minimal: the startup flow is defined in `Program.cs`, the main window is created in `App.axaml.cs`, and the UI is bound to a view model under `ViewModels/`.

## Build, test, and lint commands
- Restore packages: `dotnet restore`
- Build the app: `dotnet build FormaterXJ.csproj`
- Run the app locally: `dotnet run --project FormaterXJ.csproj`
- Test status: no dedicated test project or test runner configuration was found in this repository.
- Single-test command pattern (for future test projects): `dotnet test <TestProject>.csproj --filter "FullyQualifiedName~<TestName>"`
- Linting: no lint configuration or repo-wide lint command was detected.

## High-level architecture
- `Program.cs` is the entry point. It configures Avalonia via `BuildAvaloniaApp()` and starts the classic desktop lifetime.
- `App.axaml.cs` loads the app XAML and assigns `desktop.MainWindow` to a `MainWindow` with a `MainViewModel` as its `DataContext`.
- `Views/MainWindow.axaml` defines the actual window layout and uses XAML binding to the view model (`Greeting`). `Views/MainWindow.axaml.cs` simply calls `InitializeComponent()`.
- `ViewModels/MainViewModel.cs` is the bindable state container. It uses `CommunityToolkit.Mvvm` and `[ObservableProperty]` rather than manual property-change code.
- `ViewLocator.cs` is a small reflection-based template that maps a view model type to a corresponding view by name (`ViewModel` -> `View`). Keep matching names stable when adding new view models/views.
- The project currently has no separate `Models` layer or business logic; UI state and display concerns are the primary responsibility of the view model layer.

## Key conventions
- Keep the Avalonia startup flow in `Program.cs` and `App.axaml.cs` unless a broader app lifecycle change is explicitly required.
- Prefer MVVM patterns already in use: view model properties exposed with `[ObservableProperty]`, and XAML bindings against those properties.
- Match XAML binding property names exactly (`Greeting` -> `Text="{Binding Greeting}"`). This repo relies on the generated bindable properties and strongly typed `x:DataType` declarations.
- Keep XAML and code-behind focused on UI composition. The project is intentionally lightweight; don't add complex service containers or custom startup paths without a clear need.
- When introducing new views, follow the existing naming convention: `ViewModels/<Name>ViewModel.cs` and `Views/<Name>View.axaml` / `Views/<Name>View.axaml.cs`, and make sure the view model class name still aligns with the view locator pattern.
- For any validation task, prefer the smallest relevant local check (`dotnet build` for the app; `dotnet test --filter` when a test project exists). This repository does not appear to include a broader CI or lint gate that should be treated as mandatory without a dedicated project configuration.
