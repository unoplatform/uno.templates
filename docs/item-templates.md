# Uno Platform item templates

These `dotnet new` item templates add common files to an existing Uno Platform
project. They work from the CLI and in IDEs that surface `dotnet new` item
templates (Rider, VS Code with the C# Dev Kit). In Visual Studio, use the
built-in **Add New Item** dialog instead.

Run any of these from inside your project folder:

| Command | Adds |
| --- | --- |
| `dotnet new uno-page -n MyPage` | A `Page` (XAML + code-behind). Add `-markup csharp` for C# Markup. |
| `dotnet new uno-window -n MyWindow` | A `Window`. Add `-markup csharp` for C# Markup. |
| `dotnet new uno-usercontrol -n MyControl` | A `UserControl`. Add `-markup csharp` for C# Markup. |
| `dotnet new uno-contentdialog -n MyDialog` | A `ContentDialog`. Add `-markup csharp` for C# Markup. |
| `dotnet new uno-resourcedictionary -n MyDictionary` | A `ResourceDictionary` (no code-behind). |
| `dotnet new uno-resourcedictionary-codebehind -n MyDictionary` | A `ResourceDictionary` with code-behind. |
| `dotnet new uno-resw -n Resources` | A `.resw` resource file for localized strings. |
| `dotnet new uno-templatedcontrol -n MyControl` | A lookless `Control` plus a default style in `Themes/Generic.xaml`. Pass `-I false` to skip `Generic.xaml` when it already exists. |
| `dotnet new uno-mvvm-page -n MyPage` | A `Page` + a CommunityToolkit.Mvvm `ViewModel`. |
| `dotnet new uno-mvux-page -n MyPage` | A `Page` + an Uno.Extensions.Reactive (MVUX) `Model`. |

Pass `--root-namespace <ns>` (or `-ns <ns>`) when generating outside a project
to control the generated namespace; inside a project it is inferred from the
project's `RootNamespace`.

> C# Markup items require the host app to have the `CSharpMarkup` UnoFeature
> (created with `dotnet new unoapp -markup csharp`). The MVVM / MVUX page items
> assume the app was created with `-presentation mvvm` / `-presentation mvux`.
