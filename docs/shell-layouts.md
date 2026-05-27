# Shell layouts (`--shell-layout`)

The `unoapp` template can scaffold a menu-based application shell:

| `--shell-layout` | Adds |
| --- | --- |
| `blank` (default) | A single content page (no shell). |
| `navview` | A `NavigationView` side-menu shell. |
| `tabbar` | A tab-bar shell. |

The shell adapts to the navigation model you choose:

- **Region-based** — when the app uses dependency injection + Uno.Extensions.Navigation
  (the `recommended` preset, or `-di true -nav regions`), the shell is wired with
  Uno.Extensions navigation **regions** (`uen:Region.Attached`), and the menu items
  switch inline content regions. Requires the **Toolkit** (on by default in `recommended`).
- **Plain Frame** — when the app uses default WinUI `Frame` navigation
  (the `blank` preset, or `-nav blank` / `-di false`), the shell uses a WinUI
  `NavigationView` + `Frame` (with code-behind navigation) or a WinUI `TabView`,
  the same way the Windows App SDK templates do.

Examples:

```powershell
# Region-based NavigationView shell (recommended preset)
dotnet new unoapp --shell-layout navview

# Plain Frame TabView shell (blank preset)
dotnet new unoapp -preset blank --shell-layout tabbar
```

> **C# Markup:** shell layouts currently require XAML markup. When `-markup csharp`
> is combined with `--shell-layout`, the app is generated with the standard single
> page; C# Markup shell layouts are planned as a follow-up.
