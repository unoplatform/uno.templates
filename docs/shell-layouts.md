# Shell layouts (`--shell-layout`)

The `unoapp` template can scaffold a menu-based application shell:

| `--shell-layout` | Adds |
| --- | --- |
| `blank` (default) | A single content page (no shell). |
| `navview` | A `NavigationView` (side menu) shell. |
| `tabbar` | An Uno.Toolkit `TabBar` (bottom navigation) shell. |
| `tabview` | A WinUI `TabView` (tabbed documents) shell. |

How each behaves:

- **`navview`** and **`tabbar`** are *navigation* shells and adapt to the app's navigation model:
  - **Region-based** when the app uses dependency injection + Uno.Extensions.Navigation
    (the `recommended` preset, or `-di true -nav regions`): the shell is wired with navigation
    **regions** (`uen:Region.Attached`) and the menu items switch inline content regions.
  - **Plain Frame** otherwise (the `blank` preset, or `-nav blank`): the shell uses a WinUI
    `NavigationView` + `Frame` (`navview`) or `TabBar` + `Frame` (`tabbar`) with code-behind navigation.
  - `tabbar` uses the Uno.Toolkit `TabBar`, so it requires the **Toolkit** (enabled by default in the
    `recommended` preset; in the `blank` preset add `-toolkit true`).
- **`tabview`** uses the WinUI `TabView` (tabbed documents). It is a plain, static tabbed UI and does
  **not** participate in region navigation, so it looks the same regardless of the navigation model and
  needs neither the Toolkit nor Uno.Extensions.Navigation.

Examples:

```powershell
# Region-based NavigationView shell (recommended preset)
dotnet new unoapp --shell-layout navview

# Region-based bottom TabBar shell (recommended preset)
dotnet new unoapp --shell-layout tabbar

# Plain tabbed-documents TabView shell (any preset)
dotnet new unoapp -preset blank --shell-layout tabview
```

> **C# Markup:** shell layouts currently require XAML markup. When `-markup csharp`
> is combined with `--shell-layout`, the app is generated with the standard single
> page; C# Markup shell layouts are planned as a follow-up.
