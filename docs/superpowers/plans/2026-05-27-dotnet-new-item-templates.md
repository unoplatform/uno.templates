# Uno Platform `dotnet new` Item Templates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `dotnet new` item templates (Page, Window, UserControl, ContentDialog, ResourceDictionary, ResourceDictionary-with-code-behind, .resw, TemplatedControl, plus MVVM/MVUX page variants and C# Markup variants) to `Uno.Templates`, so "Add New Item" / `dotnet new` works in the CLI, Rider, and VS Code — not just Visual Studio.

**Architecture:** Each item template is a self-contained folder under `src/Uno.Templates/content/item-*/` containing a `.template.config/` (`template.json` + `dotnetcli.host.json` + `ide.host.json`) and the item's source files. The packaging csproj already globs `content/**/*` (`Uno.Templates.csproj:28`), so new folders are packaged automatically with **no csproj changes**. Item bodies are ported from the Visual Studio `.vstemplate` items in `D:\Work\uno.studio\src\Uno.Studio\Studio.ItemTemplates` (which already use the `$rootnamespace$` / `$safeitemname$` tokens that `dotnet new` symbols replace), then **modernized** (file-scoped namespaces, no redundant usings, matching `unoapp`-generated code style). C# Markup variants reuse `unoapp`'s `#if useCsharpMarkup` + `cnd:noEmit` + source-rename mechanism.

**Tech Stack:** .NET `dotnet new` template engine (template.json schema v3), Uno.Sdk single project, CommunityToolkit.Mvvm (MVVM), Uno.Extensions.Reactive (MVUX), Uno.Extensions.Markup (C# Markup).

**Key facts established during research:**
- `Uno.Templates.csproj:28` — `<TemplateFile Include="content/**/*" ... />` + `NoDefaultExcludes=true` (`:12`) means dot-folders like `.template.config` are packaged. New `content/item-*` folders need no registration.
- `unoapp/.template.config/ide.host.json` sets `unsupportedHosts: [{ id: "vs" }]`. We mirror this on every item template so Visual Studio keeps using the existing uno.studio `.vstemplate` items (no duplicate "Add New Item" entries), while CLI/Rider/VS Code get the new ones.
- Uno.Sdk single projects expose `Microsoft.UI.Xaml` and `Microsoft.UI.Xaml.Controls` as implicit global usings — `unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml.cs` references `Page` with **zero** `using` directives. Our modernized item code relies on this (no explicit WinUI usings), matching the surrounding generated code.
- **TemplatedControl quality bug being fixed:** the uno.studio `TemplatedControl` ships only `CustomControl.cs` with `DefaultStyleKey = typeof(...)` and **no `Themes/Generic.xaml`**, so the control renders nothing. WinAppSDK's `item-templated-control` ships a default style. We port that style.
- `sourceName` collision pitfall: `dotnet new` replaces every literal occurrence of `sourceName` in file names **and content**. So a `.resw`'s `sourceName` must NOT be `"Resources"` (the schema contains `System.Resources.ResXResourceReader`). We use `sourceName: "Strings"` + `defaultName: "Resources"`.

**Local test loop (used in every verification step):** `dotnet new` can install a template directly from a source folder — no packaging required:
```powershell
dotnet new install <repo>\src\Uno.Templates\content\item-page --force
dotnet new uno-page -n LoginPage --root-namespace Acme.App -o $env:TEMP\itemtest
```
Generation is pure text templating (no Uno workloads needed). A full `dotnet build` of a generated host app (Tasks in Phase 5) does need the Uno workloads installed.

---

## File Structure

Created under `src/Uno.Templates/content/` (one folder per template):

| Folder | shortName | Files |
|---|---|---|
| `item-page/` | `uno-page` | `.template.config/{template,dotnetcli.host,ide.host}.json`, `BlankPage.xaml`, `BlankPage.xaml.cs` |
| `item-window/` | `uno-window` | config + `BlankWindow.xaml`, `BlankWindow.xaml.cs` |
| `item-usercontrol/` | `uno-usercontrol` | config + `MyUserControl.xaml`, `MyUserControl.xaml.cs` |
| `item-contentdialog/` | `uno-contentdialog` | config + `MyContentDialog.xaml`, `MyContentDialog.xaml.cs` |
| `item-resourcedictionary/` | `uno-resourcedictionary` | config + `MyDictionary.xaml` |
| `item-resourcedictionary-codebehind/` | `uno-resourcedictionary-codebehind` | config + `MyResourceDictionary.xaml`, `MyResourceDictionary.xaml.cs` |
| `item-resw/` | `uno-resw` | config + `Strings.resw` |
| `item-templatedcontrol/` | `uno-templatedcontrol` | config + `CustomControl.cs`, `Themes/Generic.xaml` |
| `item-mvvm-page/` | `uno-mvvm-page` | config + `BlankPage.xaml`, `BlankPage.xaml.cs`, `BlankPageViewModel.cs` |
| `item-mvux-page/` | `uno-mvux-page` | config + `BlankPage.xaml`, `BlankPage.xaml.cs`, `BlankPageModel.cs` |

Also created:
- `build/test-item-templates.ps1` — integration smoke test (generate `unoapp` → add every item → build).
- `docs/item-templates.md` — user-facing list of item templates and usage.

Modified:
- `README.md` — link to `docs/item-templates.md`.
- `.github/workflows/ci.yml` — invoke the integration smoke test (exact edit determined in Task by reading the file).

**Phases:**
- **Phase 1 (Task 1):** Infrastructure + first item (`uno-page`). Establishes the pattern + local test loop.
- **Phase 2 (Tasks 2–7):** Remaining XAML/resource items, modernized.
- **Phase 3 (Task 8):** `uno-templatedcontrol` **with** `Themes/Generic.xaml` (the quality fix) + `IncludeDefaultStyle` switch.
- **Phase 4 (Tasks 9–12):** C# Markup variants (`markup` choice) on Page/Window/UserControl/ContentDialog.
- **Phase 5 (Tasks 13–14):** MVVM and MVUX paired page items.
- **Phase 6 (Tasks 15–17):** Integration smoke test, docs, CI wiring.

---

## Phase 1 — Infrastructure + first item

### Task 1: `uno-page` item template

**Files:**
- Create: `src/Uno.Templates/content/item-page/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-page/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-page/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-page/BlankPage.xaml`
- Create: `src/Uno.Templates/content/item-page/BlankPage.xaml.cs`

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-page/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Page" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.Page",
  "groupIdentity": "Uno.Platform.Item.Page",
  "name": "Uno Platform Page (Item)",
  "shortName": "uno-page",
  "sourceName": "BlankPage",
  "defaultName": "BlankPage",
  "description": "Adds an Uno Platform Page with XAML markup and code-behind.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    }
  },
  "forms": {
    "safe_name": { "identifier": "safe_name" }
  },
  "primaryOutputs": [
    { "path": "BlankPage.xaml" },
    { "path": "BlankPage.xaml.cs" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-page/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": {
      "longName": "root-namespace",
      "shortName": "ns"
    }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`** (hide from Visual Studio so it doesn't duplicate the uno.studio `.vstemplate` item)

`src/Uno.Templates/content/item-page/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [
    { "id": "vs" }
  ]
}
```

- [ ] **Step 4: Write `BlankPage.xaml`**

`src/Uno.Templates/content/item-page/BlankPage.xaml`:
```xml
<Page
    x:Class="$rootnamespace$.$safeitemname$"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:$rootnamespace$"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>

    </Grid>
</Page>
```

- [ ] **Step 5: Write modernized `BlankPage.xaml.cs`** (file-scoped namespace, no redundant usings — relies on Uno.Sdk implicit usings exactly like `unoapp`'s `MainPage.xaml.cs`)

`src/Uno.Templates/content/item-page/BlankPage.xaml.cs`:
```csharp
namespace $rootnamespace$;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class $safeitemname$ : Page
{
    public $safeitemname$()
    {
        this.InitializeComponent();
    }
}
```

- [ ] **Step 6: Install the template from source and generate (verify wiring)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-page --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-page -n LoginPage --root-namespace Acme.App -o $env:TEMP\itemtest
Get-ChildItem $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\LoginPage.xaml.cs
```
Expected:
- Files `LoginPage.xaml` and `LoginPage.xaml.cs` exist (renamed from `BlankPage`).
- `LoginPage.xaml.cs` contains `namespace Acme.App;` and `public sealed partial class LoginPage : Page`.
- `LoginPage.xaml` contains `x:Class="Acme.App.LoginPage"`.

- [ ] **Step 7: Commit**

```powershell
git add src/Uno.Templates/content/item-page
git commit -m "feat(templates): add uno-page dotnet new item template"
```

---

## Phase 2 — Remaining XAML / resource items

### Task 2: `uno-window` item template

**Files:**
- Create: `src/Uno.Templates/content/item-window/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-window/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-window/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-window/BlankWindow.xaml`
- Create: `src/Uno.Templates/content/item-window/BlankWindow.xaml.cs`

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-window/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Window" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.Window",
  "groupIdentity": "Uno.Platform.Item.Window",
  "name": "Uno Platform Window (Item)",
  "shortName": "uno-window",
  "sourceName": "BlankWindow",
  "defaultName": "BlankWindow",
  "description": "Adds an Uno Platform Window that can be activated on its own (e.g. a secondary window).",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "primaryOutputs": [
    { "path": "BlankWindow.xaml" },
    { "path": "BlankWindow.xaml.cs" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-window/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-window/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `BlankWindow.xaml`**

`src/Uno.Templates/content/item-window/BlankWindow.xaml`:
```xml
<Window
    x:Class="$rootnamespace$.$safeitemname$"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:$rootnamespace$"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid>

    </Grid>
</Window>
```

- [ ] **Step 5: Write modernized `BlankWindow.xaml.cs`**

`src/Uno.Templates/content/item-window/BlankWindow.xaml.cs`:
```csharp
namespace $rootnamespace$;

/// <summary>
/// An empty window that can be activated on its own (e.g. a secondary window of the app).
/// </summary>
public sealed partial class $safeitemname$ : Window
{
    public $safeitemname$()
    {
        this.InitializeComponent();
    }
}
```

- [ ] **Step 6: Install and generate (verify)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-window --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-window -n SecondaryWindow --root-namespace Acme.App -o $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\SecondaryWindow.xaml.cs
```
Expected: files `SecondaryWindow.xaml`/`SecondaryWindow.xaml.cs`; class `SecondaryWindow : Window`; `namespace Acme.App;`.

- [ ] **Step 7: Commit**

```powershell
git add src/Uno.Templates/content/item-window
git commit -m "feat(templates): add uno-window dotnet new item template"
```

---

### Task 3: `uno-usercontrol` item template

**Files:**
- Create: `src/Uno.Templates/content/item-usercontrol/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-usercontrol/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-usercontrol/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-usercontrol/MyUserControl.xaml`
- Create: `src/Uno.Templates/content/item-usercontrol/MyUserControl.xaml.cs`

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-usercontrol/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "User Control" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.UserControl",
  "groupIdentity": "Uno.Platform.Item.UserControl",
  "name": "Uno Platform User Control (Item)",
  "shortName": "uno-usercontrol",
  "sourceName": "MyUserControl",
  "defaultName": "MyUserControl",
  "description": "Adds an Uno Platform UserControl for reusable composition.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "primaryOutputs": [
    { "path": "MyUserControl.xaml" },
    { "path": "MyUserControl.xaml.cs" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-usercontrol/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-usercontrol/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `MyUserControl.xaml`**

`src/Uno.Templates/content/item-usercontrol/MyUserControl.xaml`:
```xml
<UserControl
    x:Class="$rootnamespace$.$safeitemname$"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:$rootnamespace$"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    d:DesignHeight="300"
    d:DesignWidth="400">

    <Grid>

    </Grid>
</UserControl>
```

- [ ] **Step 5: Write modernized `MyUserControl.xaml.cs`**

`src/Uno.Templates/content/item-usercontrol/MyUserControl.xaml.cs`:
```csharp
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : UserControl
{
    public $safeitemname$()
    {
        this.InitializeComponent();
    }
}
```

- [ ] **Step 6: Install and generate (verify)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-usercontrol --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-usercontrol -n RatingControl --root-namespace Acme.App -o $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\RatingControl.xaml.cs
```
Expected: `RatingControl.xaml`/`.xaml.cs`; class `RatingControl : UserControl`; `namespace Acme.App;`.

- [ ] **Step 7: Commit**

```powershell
git add src/Uno.Templates/content/item-usercontrol
git commit -m "feat(templates): add uno-usercontrol dotnet new item template"
```

---

### Task 4: `uno-contentdialog` item template

**Files:**
- Create: `src/Uno.Templates/content/item-contentdialog/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-contentdialog/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-contentdialog/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-contentdialog/MyContentDialog.xaml`
- Create: `src/Uno.Templates/content/item-contentdialog/MyContentDialog.xaml.cs`

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-contentdialog/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Content Dialog" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.ContentDialog",
  "groupIdentity": "Uno.Platform.Item.ContentDialog",
  "name": "Uno Platform Content Dialog (Item)",
  "shortName": "uno-contentdialog",
  "sourceName": "MyContentDialog",
  "defaultName": "MyContentDialog",
  "description": "Adds an Uno Platform ContentDialog for modal interactions.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "primaryOutputs": [
    { "path": "MyContentDialog.xaml" },
    { "path": "MyContentDialog.xaml.cs" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-contentdialog/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-contentdialog/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `MyContentDialog.xaml`**

`src/Uno.Templates/content/item-contentdialog/MyContentDialog.xaml`:
```xml
<ContentDialog
    x:Class="$rootnamespace$.$safeitemname$"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:$rootnamespace$"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    Title="Title"
    PrimaryButtonText="Primary"
    SecondaryButtonText="Secondary"
    PrimaryButtonClick="ContentDialog_PrimaryButtonClick"
    SecondaryButtonClick="ContentDialog_SecondaryButtonClick">

    <Grid>

    </Grid>
</ContentDialog>
```

- [ ] **Step 5: Write modernized `MyContentDialog.xaml.cs`**

`src/Uno.Templates/content/item-contentdialog/MyContentDialog.xaml.cs`:
```csharp
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : ContentDialog
{
    public $safeitemname$()
    {
        this.InitializeComponent();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }
}
```

- [ ] **Step 6: Install and generate (verify)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-contentdialog --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-contentdialog -n DeleteDialog --root-namespace Acme.App -o $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\DeleteDialog.xaml.cs
```
Expected: `DeleteDialog.xaml`/`.xaml.cs`; class `DeleteDialog : ContentDialog`; both click handlers present; `namespace Acme.App;`.

- [ ] **Step 7: Commit**

```powershell
git add src/Uno.Templates/content/item-contentdialog
git commit -m "feat(templates): add uno-contentdialog dotnet new item template"
```

---

### Task 5: `uno-resourcedictionary` item template (no code-behind)

**Files:**
- Create: `src/Uno.Templates/content/item-resourcedictionary/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-resourcedictionary/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-resourcedictionary/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-resourcedictionary/MyDictionary.xaml`

> Note: `sourceName` is `MyDictionary` (NOT `ResourceDictionary` — that string appears in the XAML root element and would be corrupted). The XAML has no `x:Class`, so `$rootnamespace$`/`$safeitemname$` are unused here; `sourceName` only renames the file.

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-resourcedictionary/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Resource Dictionary" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.ResourceDictionary",
  "groupIdentity": "Uno.Platform.Item.ResourceDictionary",
  "name": "Uno Platform Resource Dictionary (Item)",
  "shortName": "uno-resourcedictionary",
  "sourceName": "MyDictionary",
  "defaultName": "MyDictionary",
  "description": "Adds an Uno Platform ResourceDictionary for shared styles and resources.",
  "preferNameDirectory": false,
  "primaryOutputs": [
    { "path": "MyDictionary.xaml" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-resourcedictionary/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {}
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-resourcedictionary/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `MyDictionary.xaml`**

`src/Uno.Templates/content/item-resourcedictionary/MyDictionary.xaml`:
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

</ResourceDictionary>
```

- [ ] **Step 5: Install and generate (verify)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-resourcedictionary --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-resourcedictionary -n Colors -o $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\Colors.xaml
```
Expected: file `Colors.xaml`; content is a valid `<ResourceDictionary>` with the root element **intact** (not corrupted by name replacement).

- [ ] **Step 6: Commit**

```powershell
git add src/Uno.Templates/content/item-resourcedictionary
git commit -m "feat(templates): add uno-resourcedictionary dotnet new item template"
```

---

### Task 6: `uno-resourcedictionary-codebehind` item template

**Files:**
- Create: `src/Uno.Templates/content/item-resourcedictionary-codebehind/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-resourcedictionary-codebehind/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-resourcedictionary-codebehind/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-resourcedictionary-codebehind/MyResourceDictionary.xaml`
- Create: `src/Uno.Templates/content/item-resourcedictionary-codebehind/MyResourceDictionary.xaml.cs`

> Note: `sourceName` is `MyResourceDictionary` — the literal string `MyResourceDictionary` does NOT occur inside `<ResourceDictionary` or `: ResourceDictionary`, so the base type stays intact. The class name comes from the `$safeitemname$` token.

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-resourcedictionary-codebehind/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Resource Dictionary" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.ResourceDictionaryCodeBehind",
  "groupIdentity": "Uno.Platform.Item.ResourceDictionaryCodeBehind",
  "name": "Uno Platform Resource Dictionary with code-behind (Item)",
  "shortName": "uno-resourcedictionary-codebehind",
  "sourceName": "MyResourceDictionary",
  "defaultName": "MyResourceDictionary",
  "description": "Adds an Uno Platform ResourceDictionary with a code-behind partial class.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "primaryOutputs": [
    { "path": "MyResourceDictionary.xaml" },
    { "path": "MyResourceDictionary.xaml.cs" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-resourcedictionary-codebehind/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-resourcedictionary-codebehind/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `MyResourceDictionary.xaml`**

`src/Uno.Templates/content/item-resourcedictionary-codebehind/MyResourceDictionary.xaml`:
```xml
<ResourceDictionary
    x:Class="$rootnamespace$.$safeitemname$"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

</ResourceDictionary>
```

- [ ] **Step 5: Write modernized `MyResourceDictionary.xaml.cs`**

`src/Uno.Templates/content/item-resourcedictionary-codebehind/MyResourceDictionary.xaml.cs`:
```csharp
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : ResourceDictionary
{
    public $safeitemname$()
    {
        this.InitializeComponent();
    }
}
```

- [ ] **Step 6: Install and generate (verify)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-resourcedictionary-codebehind --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-resourcedictionary-codebehind -n Theme --root-namespace Acme.App -o $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\Theme.xaml.cs
Get-Content $env:TEMP\itemtest\Theme.xaml
```
Expected: `Theme.xaml`/`.xaml.cs`; class `Theme : ResourceDictionary` (base type intact); `x:Class="Acme.App.Theme"`; `namespace Acme.App;`.

- [ ] **Step 7: Commit**

```powershell
git add src/Uno.Templates/content/item-resourcedictionary-codebehind
git commit -m "feat(templates): add uno-resourcedictionary-codebehind dotnet new item template"
```

---

### Task 7: `uno-resw` item template

**Files:**
- Create: `src/Uno.Templates/content/item-resw/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-resw/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-resw/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-resw/Strings.resw`

> Note: `sourceName` is `Strings` and `defaultName` is `Resources`. We must NOT use `Resources` as `sourceName` — the `.resw` schema contains `System.Resources.ResXResourceReader`/`...Writer`, which would be corrupted. With `sourceName: "Strings"` (absent from content) and `defaultName: "Resources"`, `dotnet new uno-resw` produces `Resources.resw` with pristine content.

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-resw/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Resources" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.Resw",
  "groupIdentity": "Uno.Platform.Item.Resw",
  "name": "Uno Platform Resources File (.resw)",
  "shortName": "uno-resw",
  "sourceName": "Strings",
  "defaultName": "Resources",
  "description": "Adds a .resw resource file for localized strings.",
  "preferNameDirectory": false,
  "primaryOutputs": [
    { "path": "Strings.resw" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-resw/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {}
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-resw/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `Strings.resw`** (standard empty .resw with the resx schema header)

`src/Uno.Templates/content/item-resw/Strings.resw`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
</root>
```

- [ ] **Step 5: Install and generate (verify content not corrupted)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-resw --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-resw -o $env:TEMP\itemtest
Get-ChildItem $env:TEMP\itemtest
Select-String -Path $env:TEMP\itemtest\Resources.resw -Pattern "System.Resources.ResXResourceReader"
```
Expected: file is named `Resources.resw` (from `defaultName`); the `System.Resources.ResXResourceReader` line is present and **unmodified**.

- [ ] **Step 6: Commit**

```powershell
git add src/Uno.Templates/content/item-resw
git commit -m "feat(templates): add uno-resw dotnet new item template"
```

---

## Phase 3 — Templated control (with the `Themes/Generic.xaml` quality fix)

### Task 8: `uno-templatedcontrol` item template

**Files:**
- Create: `src/Uno.Templates/content/item-templatedcontrol/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-templatedcontrol/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-templatedcontrol/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-templatedcontrol/CustomControl.cs`
- Create: `src/Uno.Templates/content/item-templatedcontrol/Themes/Generic.xaml`

> Fixes the uno.studio gap: ships a default `Themes/Generic.xaml` style so the control is visible. `IncludeDefaultStyle` (default `true`) lets a second invocation skip `Themes/Generic.xaml` to avoid overwriting an existing one (`dotnet new uno-templatedcontrol -n Second -I false`).

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-templatedcontrol/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Templated Control" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.TemplatedControl",
  "groupIdentity": "Uno.Platform.Item.TemplatedControl",
  "name": "Uno Platform Templated Control (Item)",
  "shortName": "uno-templatedcontrol",
  "sourceName": "CustomControl",
  "defaultName": "CustomControl",
  "description": "Adds an Uno Platform lookless Control with a default style in Themes/Generic.xaml.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    },
    "IncludeDefaultStyle": {
      "type": "parameter",
      "datatype": "bool",
      "defaultValue": "true",
      "description": "Generate a default style in Themes/Generic.xaml. Set to false when the project already has a Themes/Generic.xaml you do not want to overwrite."
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "sources": [
    {
      "modifiers": [
        {
          "condition": "(!IncludeDefaultStyle)",
          "exclude": [ "Themes/Generic.xaml" ]
        }
      ]
    }
  ],
  "primaryOutputs": [
    { "path": "CustomControl.cs" },
    { "path": "Themes/Generic.xaml", "condition": "(IncludeDefaultStyle)" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`** (maps `IncludeDefaultStyle` to `-I` / `--include-default-style`)

`src/Uno.Templates/content/item-templatedcontrol/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" },
    "IncludeDefaultStyle": { "longName": "include-default-style", "shortName": "I" }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-templatedcontrol/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write modernized `CustomControl.cs`**

`src/Uno.Templates/content/item-templatedcontrol/CustomControl.cs`:
```csharp
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : Control
{
    public $safeitemname$()
    {
        this.DefaultStyleKey = typeof($safeitemname$);
    }
}
```

- [ ] **Step 5: Write `Themes/Generic.xaml`** (the fix — a working default style/template)

`src/Uno.Templates/content/item-templatedcontrol/Themes/Generic.xaml`:
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:$rootnamespace$">

    <!--
        To add another templated control to this project, re-run with the
        IncludeDefaultStyle option set to false so this file is not overwritten:

            dotnet new uno-templatedcontrol -n NewControl -I false

        Then copy the Style block below, change every "local:$safeitemname$" to
        "local:NewControl", and customize the template.
    -->

    <Style TargetType="local:$safeitemname$">
        <Setter Property="HorizontalContentAlignment" Value="Stretch" />
        <Setter Property="VerticalContentAlignment" Value="Stretch" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="local:$safeitemname$">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="{TemplateBinding CornerRadius}"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
                                          VerticalAlignment="{TemplateBinding VerticalContentAlignment}" />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>
```

- [ ] **Step 6: Install and generate — default (with style)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-templatedcontrol --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-templatedcontrol -n RatingBar --root-namespace Acme.App -o $env:TEMP\itemtest
Get-ChildItem -Recurse $env:TEMP\itemtest | Select-Object FullName
Get-Content $env:TEMP\itemtest\Themes\Generic.xaml
```
Expected: `RatingBar.cs` + `Themes\Generic.xaml`; the style has `TargetType="local:RatingBar"` and `xmlns:local="using:Acme.App"`.

- [ ] **Step 7: Generate again with `-I false` (no style) — verify exclusion**

Run:
```powershell
Remove-Item -Recurse -Force $env:TEMP\itemtest2 -ErrorAction SilentlyContinue
dotnet new uno-templatedcontrol -n SecondControl --root-namespace Acme.App -I false -o $env:TEMP\itemtest2
Get-ChildItem -Recurse $env:TEMP\itemtest2 | Select-Object FullName
```
Expected: only `SecondControl.cs` is generated; **no** `Themes\Generic.xaml`.

- [ ] **Step 8: Commit**

```powershell
git add src/Uno.Templates/content/item-templatedcontrol
git commit -m "feat(templates): add uno-templatedcontrol item template with default Themes/Generic.xaml"
```

---

## Phase 4 — C# Markup variants

Adds a `markup` choice (`xaml` default, `csharp`) to the code-behind items. With `-markup csharp`, the template renames `<Item>.xaml.cs` → `<Item>.cs`, excludes the `.xaml`, and the constructor body switches from `InitializeComponent()` to a C# Markup `.Content(...)` builder via the `#if useCsharpMarkup` block. This mirrors `unoapp`'s mechanism (`template.json` source modifiers + `//-:cnd:noEmit` / `//+:cnd:noEmit` markers; see `unoapp/.template.config/template.json:2826-2842` and `unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml.cs`).

> **Prerequisite for the generated code to compile:** the host app must have the `CSharpMarkup` UnoFeature (i.e. created with `dotnet new unoapp -markup csharp`), which provides the Uno.Extensions.Markup global usings. The XAML mode has no such requirement.

### Task 9: Add `markup` choice to `uno-page`

**Files:**
- Modify: `src/Uno.Templates/content/item-page/.template.config/template.json`
- Modify: `src/Uno.Templates/content/item-page/.template.config/dotnetcli.host.json`
- Modify: `src/Uno.Templates/content/item-page/BlankPage.xaml.cs`

- [ ] **Step 1: Update `template.json`** — add the `markup`/`useCsharpMarkup` symbols and source modifiers

Replace the contents of `src/Uno.Templates/content/item-page/.template.config/template.json` with:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Page" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.Page",
  "groupIdentity": "Uno.Platform.Item.Page",
  "name": "Uno Platform Page (Item)",
  "shortName": "uno-page",
  "sourceName": "BlankPage",
  "defaultName": "BlankPage",
  "description": "Adds an Uno Platform Page with XAML or C# Markup.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    },
    "markup": {
      "type": "parameter",
      "datatype": "choice",
      "defaultValue": "xaml",
      "description": "The UI markup language for the page.",
      "choices": [
        { "choice": "xaml", "description": "XAML markup with code-behind" },
        { "choice": "csharp", "description": "C# Markup (single file)" }
      ]
    },
    "useCsharpMarkup": {
      "type": "computed",
      "value": "(markup == 'csharp')"
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "sources": [
    {
      "modifiers": [
        {
          "condition": "(useCsharpMarkup)",
          "exclude": [ "BlankPage.xaml" ],
          "rename": { "BlankPage.xaml.cs": "BlankPage.cs" }
        }
      ]
    }
  ],
  "primaryOutputs": [
    { "path": "BlankPage.xaml", "condition": "(!useCsharpMarkup)" },
    { "path": "BlankPage.xaml.cs", "condition": "(!useCsharpMarkup)" },
    { "path": "BlankPage.cs", "condition": "(useCsharpMarkup)" }
  ]
}
```

- [ ] **Step 2: Update `dotnetcli.host.json`** — expose `-markup`

Replace `src/Uno.Templates/content/item-page/.template.config/dotnetcli.host.json` with:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" },
    "markup": { "longName": "markup", "shortName": "markup" }
  }
}
```

- [ ] **Step 3: Update `BlankPage.xaml.cs`** — dual-mode body with `cnd:noEmit` markers

Replace `src/Uno.Templates/content/item-page/BlankPage.xaml.cs` with:
```csharp
//-:cnd:noEmit
namespace $rootnamespace$;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class $safeitemname$ : Page
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Content(
            new StackPanel()
                .HorizontalAlignment(HorizontalAlignment.Center)
                .VerticalAlignment(VerticalAlignment.Center)
                .Children(
                    new TextBlock().Text("Hello Uno Platform!")));
#else
        this.InitializeComponent();
#endif
    }
}
```

- [ ] **Step 4: Verify XAML mode still works (default)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-page --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-page -n LoginPage --root-namespace Acme.App -o $env:TEMP\itemtest
Get-ChildItem $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\LoginPage.xaml.cs
```
Expected: `LoginPage.xaml` + `LoginPage.xaml.cs`; the `.xaml.cs` contains `this.InitializeComponent();` and **no** `#if` directives (the engine resolved them).

- [ ] **Step 5: Verify C# Markup mode**

Run:
```powershell
Remove-Item -Recurse -Force $env:TEMP\itemtest2 -ErrorAction SilentlyContinue
dotnet new uno-page -n LoginPage --root-namespace Acme.App -markup csharp -o $env:TEMP\itemtest2
Get-ChildItem $env:TEMP\itemtest2
Get-Content $env:TEMP\itemtest2\LoginPage.cs
```
Expected: only `LoginPage.cs` (no `.xaml`); body contains the `.Content(new StackPanel()...)` builder and **no** `#if`/`InitializeComponent`.

- [ ] **Step 6: Commit**

```powershell
git add src/Uno.Templates/content/item-page
git commit -m "feat(templates): add C# Markup variant to uno-page item template"
```

### Task 10: Add `markup` choice to `uno-window`

**Files:**
- Modify: `src/Uno.Templates/content/item-window/.template.config/template.json`
- Modify: `src/Uno.Templates/content/item-window/.template.config/dotnetcli.host.json`
- Modify: `src/Uno.Templates/content/item-window/BlankWindow.xaml.cs`

- [ ] **Step 1: Update `template.json`** (same shape as Task 9, with Window paths)

Replace `src/Uno.Templates/content/item-window/.template.config/template.json`'s `symbols`, add `sources`, and replace `primaryOutputs` so the file reads:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Window" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.Window",
  "groupIdentity": "Uno.Platform.Item.Window",
  "name": "Uno Platform Window (Item)",
  "shortName": "uno-window",
  "sourceName": "BlankWindow",
  "defaultName": "BlankWindow",
  "description": "Adds an Uno Platform Window with XAML or C# Markup.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    },
    "markup": {
      "type": "parameter",
      "datatype": "choice",
      "defaultValue": "xaml",
      "description": "The UI markup language for the window.",
      "choices": [
        { "choice": "xaml", "description": "XAML markup with code-behind" },
        { "choice": "csharp", "description": "C# Markup (single file)" }
      ]
    },
    "useCsharpMarkup": { "type": "computed", "value": "(markup == 'csharp')" }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "sources": [
    {
      "modifiers": [
        {
          "condition": "(useCsharpMarkup)",
          "exclude": [ "BlankWindow.xaml" ],
          "rename": { "BlankWindow.xaml.cs": "BlankWindow.cs" }
        }
      ]
    }
  ],
  "primaryOutputs": [
    { "path": "BlankWindow.xaml", "condition": "(!useCsharpMarkup)" },
    { "path": "BlankWindow.xaml.cs", "condition": "(!useCsharpMarkup)" },
    { "path": "BlankWindow.cs", "condition": "(useCsharpMarkup)" }
  ]
}
```

- [ ] **Step 2: Update `dotnetcli.host.json`**

Replace `src/Uno.Templates/content/item-window/.template.config/dotnetcli.host.json` with:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" },
    "markup": { "longName": "markup", "shortName": "markup" }
  }
}
```

- [ ] **Step 3: Update `BlankWindow.xaml.cs`** — dual-mode body

Replace `src/Uno.Templates/content/item-window/BlankWindow.xaml.cs` with:
```csharp
//-:cnd:noEmit
namespace $rootnamespace$;

/// <summary>
/// An empty window that can be activated on its own (e.g. a secondary window of the app).
/// </summary>
public sealed partial class $safeitemname$ : Window
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Content(
            new Grid()
                .Children(
                    new TextBlock()
                        .Text("Hello Uno Platform!")
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)));
#else
        this.InitializeComponent();
#endif
    }
}
```

- [ ] **Step 4: Verify both modes**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-window --force
Remove-Item -Recurse -Force $env:TEMP\it_x,$env:TEMP\it_c -ErrorAction SilentlyContinue
dotnet new uno-window -n SecondWindow --root-namespace Acme.App -o $env:TEMP\it_x
dotnet new uno-window -n SecondWindow --root-namespace Acme.App -markup csharp -o $env:TEMP\it_c
Get-ChildItem $env:TEMP\it_x; Get-ChildItem $env:TEMP\it_c
```
Expected: `it_x` has `SecondWindow.xaml`+`.xaml.cs` (with `InitializeComponent`); `it_c` has only `SecondWindow.cs` (with `.Content(...)`).

- [ ] **Step 5: Commit**

```powershell
git add src/Uno.Templates/content/item-window
git commit -m "feat(templates): add C# Markup variant to uno-window item template"
```

### Task 11: Add `markup` choice to `uno-usercontrol`

**Files:**
- Modify: `src/Uno.Templates/content/item-usercontrol/.template.config/template.json`
- Modify: `src/Uno.Templates/content/item-usercontrol/.template.config/dotnetcli.host.json`
- Modify: `src/Uno.Templates/content/item-usercontrol/MyUserControl.xaml.cs`

- [ ] **Step 1: Update `template.json`**

Replace `src/Uno.Templates/content/item-usercontrol/.template.config/template.json` with:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "User Control" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.UserControl",
  "groupIdentity": "Uno.Platform.Item.UserControl",
  "name": "Uno Platform User Control (Item)",
  "shortName": "uno-usercontrol",
  "sourceName": "MyUserControl",
  "defaultName": "MyUserControl",
  "description": "Adds an Uno Platform UserControl with XAML or C# Markup.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    },
    "markup": {
      "type": "parameter",
      "datatype": "choice",
      "defaultValue": "xaml",
      "description": "The UI markup language for the control.",
      "choices": [
        { "choice": "xaml", "description": "XAML markup with code-behind" },
        { "choice": "csharp", "description": "C# Markup (single file)" }
      ]
    },
    "useCsharpMarkup": { "type": "computed", "value": "(markup == 'csharp')" }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "sources": [
    {
      "modifiers": [
        {
          "condition": "(useCsharpMarkup)",
          "exclude": [ "MyUserControl.xaml" ],
          "rename": { "MyUserControl.xaml.cs": "MyUserControl.cs" }
        }
      ]
    }
  ],
  "primaryOutputs": [
    { "path": "MyUserControl.xaml", "condition": "(!useCsharpMarkup)" },
    { "path": "MyUserControl.xaml.cs", "condition": "(!useCsharpMarkup)" },
    { "path": "MyUserControl.cs", "condition": "(useCsharpMarkup)" }
  ]
}
```

- [ ] **Step 2: Update `dotnetcli.host.json`**

Replace `src/Uno.Templates/content/item-usercontrol/.template.config/dotnetcli.host.json` with:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" },
    "markup": { "longName": "markup", "shortName": "markup" }
  }
}
```

- [ ] **Step 3: Update `MyUserControl.xaml.cs`** — dual-mode body

Replace `src/Uno.Templates/content/item-usercontrol/MyUserControl.xaml.cs` with:
```csharp
//-:cnd:noEmit
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : UserControl
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Content(new Grid());
#else
        this.InitializeComponent();
#endif
    }
}
```

- [ ] **Step 4: Verify both modes**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-usercontrol --force
Remove-Item -Recurse -Force $env:TEMP\it_x,$env:TEMP\it_c -ErrorAction SilentlyContinue
dotnet new uno-usercontrol -n Card --root-namespace Acme.App -o $env:TEMP\it_x
dotnet new uno-usercontrol -n Card --root-namespace Acme.App -markup csharp -o $env:TEMP\it_c
Get-ChildItem $env:TEMP\it_x; Get-ChildItem $env:TEMP\it_c
```
Expected: `it_x` → `Card.xaml`+`.xaml.cs`; `it_c` → `Card.cs` only.

- [ ] **Step 5: Commit**

```powershell
git add src/Uno.Templates/content/item-usercontrol
git commit -m "feat(templates): add C# Markup variant to uno-usercontrol item template"
```

### Task 12: Add `markup` choice to `uno-contentdialog`

**Files:**
- Modify: `src/Uno.Templates/content/item-contentdialog/.template.config/template.json`
- Modify: `src/Uno.Templates/content/item-contentdialog/.template.config/dotnetcli.host.json`
- Modify: `src/Uno.Templates/content/item-contentdialog/MyContentDialog.xaml.cs`

- [ ] **Step 1: Update `template.json`**

Replace `src/Uno.Templates/content/item-contentdialog/.template.config/template.json` with:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Content Dialog" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.ContentDialog",
  "groupIdentity": "Uno.Platform.Item.ContentDialog",
  "name": "Uno Platform Content Dialog (Item)",
  "shortName": "uno-contentdialog",
  "sourceName": "MyContentDialog",
  "defaultName": "MyContentDialog",
  "description": "Adds an Uno Platform ContentDialog with XAML or C# Markup.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    },
    "markup": {
      "type": "parameter",
      "datatype": "choice",
      "defaultValue": "xaml",
      "description": "The UI markup language for the dialog.",
      "choices": [
        { "choice": "xaml", "description": "XAML markup with code-behind" },
        { "choice": "csharp", "description": "C# Markup (single file)" }
      ]
    },
    "useCsharpMarkup": { "type": "computed", "value": "(markup == 'csharp')" }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "sources": [
    {
      "modifiers": [
        {
          "condition": "(useCsharpMarkup)",
          "exclude": [ "MyContentDialog.xaml" ],
          "rename": { "MyContentDialog.xaml.cs": "MyContentDialog.cs" }
        }
      ]
    }
  ],
  "primaryOutputs": [
    { "path": "MyContentDialog.xaml", "condition": "(!useCsharpMarkup)" },
    { "path": "MyContentDialog.xaml.cs", "condition": "(!useCsharpMarkup)" },
    { "path": "MyContentDialog.cs", "condition": "(useCsharpMarkup)" }
  ]
}
```

- [ ] **Step 2: Update `dotnetcli.host.json`**

Replace `src/Uno.Templates/content/item-contentdialog/.template.config/dotnetcli.host.json` with:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" },
    "markup": { "longName": "markup", "shortName": "markup" }
  }
}
```

- [ ] **Step 3: Update `MyContentDialog.xaml.cs`** — dual-mode body

Replace `src/Uno.Templates/content/item-contentdialog/MyContentDialog.xaml.cs` with:
```csharp
//-:cnd:noEmit
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : ContentDialog
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Title("Title")
            .PrimaryButtonText("Primary")
            .SecondaryButtonText("Secondary")
            .Content(new Grid());
#else
        this.InitializeComponent();
#endif
    }
#if (!useCsharpMarkup)

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }
#endif
}
```

- [ ] **Step 4: Verify both modes**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-contentdialog --force
Remove-Item -Recurse -Force $env:TEMP\it_x,$env:TEMP\it_c -ErrorAction SilentlyContinue
dotnet new uno-contentdialog -n ConfirmDialog --root-namespace Acme.App -o $env:TEMP\it_x
dotnet new uno-contentdialog -n ConfirmDialog --root-namespace Acme.App -markup csharp -o $env:TEMP\it_c
Get-Content $env:TEMP\it_x\ConfirmDialog.xaml.cs
Get-Content $env:TEMP\it_c\ConfirmDialog.cs
```
Expected: `it_x` → XAML + code-behind with both click handlers; `it_c` → single `ConfirmDialog.cs` with the `.Title(...).Content(...)` builder and **no** click-handler methods.

- [ ] **Step 5: Commit**

```powershell
git add src/Uno.Templates/content/item-contentdialog
git commit -m "feat(templates): add C# Markup variant to uno-contentdialog item template"
```

---

## Phase 5 — MVVM / MVUX paired page items

These create a Page **and** its presentation class, with the `DataContext` wired in code-behind. They assume the host app uses the matching presentation framework (`dotnet new unoapp -presentation mvvm` provides CommunityToolkit.Mvvm + global usings `CommunityToolkit.Mvvm.ComponentModel`/`.Input`; `-presentation mvux` provides Uno.Extensions.Reactive).

### Task 13: `uno-mvvm-page` item template

**Files:**
- Create: `src/Uno.Templates/content/item-mvvm-page/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-mvvm-page/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-mvvm-page/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-mvvm-page/BlankPage.xaml`
- Create: `src/Uno.Templates/content/item-mvvm-page/BlankPage.xaml.cs`
- Create: `src/Uno.Templates/content/item-mvvm-page/BlankPageViewModel.cs`

- [ ] **Step 1: Write `template.json`** (emits three files; ViewModel name = `<name>ViewModel`)

`src/Uno.Templates/content/item-mvvm-page/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Page", "MVVM" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.MvvmPage",
  "groupIdentity": "Uno.Platform.Item.MvvmPage",
  "name": "Uno Platform Page with View Model (MVVM)",
  "shortName": "uno-mvvm-page",
  "sourceName": "BlankPage",
  "defaultName": "BlankPage",
  "description": "Adds an Uno Platform Page and a CommunityToolkit.Mvvm ViewModel.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "primaryOutputs": [
    { "path": "BlankPage.xaml" },
    { "path": "BlankPage.xaml.cs" },
    { "path": "BlankPageViewModel.cs" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-mvvm-page/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-mvvm-page/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `BlankPage.xaml`** (binds the ViewModel's `Title`)

`src/Uno.Templates/content/item-mvvm-page/BlankPage.xaml`:
```xml
<Page
    x:Class="$rootnamespace$.$safeitemname$"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:$rootnamespace$"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <TextBlock
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Text="{Binding Title}" />
    </Grid>
</Page>
```

- [ ] **Step 5: Write `BlankPage.xaml.cs`** (sets `DataContext`)

`src/Uno.Templates/content/item-mvvm-page/BlankPage.xaml.cs`:
```csharp
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : Page
{
    public $safeitemname$()
    {
        this.InitializeComponent();
        this.DataContext = new $safeitemname$ViewModel();
    }
}
```

- [ ] **Step 6: Write `BlankPageViewModel.cs`** (CommunityToolkit.Mvvm)

`src/Uno.Templates/content/item-mvvm-page/BlankPageViewModel.cs`:
```csharp
namespace $rootnamespace$;

public partial class $safeitemname$ViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "$safeitemname$";
}
```

- [ ] **Step 7: Install and generate (verify)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-mvvm-page --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-mvvm-page -n Dashboard --root-namespace Acme.App -o $env:TEMP\itemtest
Get-ChildItem $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\DashboardViewModel.cs
Get-Content $env:TEMP\itemtest\Dashboard.xaml.cs
```
Expected: `Dashboard.xaml`, `Dashboard.xaml.cs`, `DashboardViewModel.cs`; code-behind sets `new DashboardViewModel()`; ViewModel class `DashboardViewModel : ObservableObject` with `[ObservableProperty] private string _title = "Dashboard";`.

- [ ] **Step 8: Commit**

```powershell
git add src/Uno.Templates/content/item-mvvm-page
git commit -m "feat(templates): add uno-mvvm-page item template"
```

### Task 14: `uno-mvux-page` item template

**Files:**
- Create: `src/Uno.Templates/content/item-mvux-page/.template.config/template.json`
- Create: `src/Uno.Templates/content/item-mvux-page/.template.config/dotnetcli.host.json`
- Create: `src/Uno.Templates/content/item-mvux-page/.template.config/ide.host.json`
- Create: `src/Uno.Templates/content/item-mvux-page/BlankPage.xaml`
- Create: `src/Uno.Templates/content/item-mvux-page/BlankPage.xaml.cs`
- Create: `src/Uno.Templates/content/item-mvux-page/BlankPageModel.cs`

> MVUX wiring caveat: Uno.Extensions.Reactive's generator emits a `Bindable<ModelName>` class in the model's namespace. The code-behind below sets `DataContext = new Bindable$safeitemname$Model();`. **This step's compile verification (Phase 5 build, Task 16) is the source of truth** — if the generated bindable constructor differs, adjust the code-behind accordingly during that build.

- [ ] **Step 1: Write `template.json`**

`src/Uno.Templates/content/item-mvux-page/.template.config/template.json`:
```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Uno Platform",
  "classifications": [ "Uno Platform", "Item", "Page", "MVUX" ],
  "tags": { "language": "C#", "type": "item" },
  "identity": "Uno.Platform.Item.MvuxPage",
  "groupIdentity": "Uno.Platform.Item.MvuxPage",
  "name": "Uno Platform Page with Model (MVUX)",
  "shortName": "uno-mvux-page",
  "sourceName": "BlankPage",
  "defaultName": "BlankPage",
  "description": "Adds an Uno Platform Page and an Uno.Extensions.Reactive (MVUX) Model.",
  "preferNameDirectory": false,
  "symbols": {
    "rootNamespace": {
      "type": "parameter",
      "datatype": "string",
      "replaces": "$rootnamespace$",
      "defaultValue": "UnoApp",
      "description": "Namespace for the generated item (pass --root-namespace / -ns to override)."
    },
    "safeItemName": {
      "type": "derived",
      "valueSource": "name",
      "valueTransform": "safe_name",
      "replaces": "$safeitemname$"
    }
  },
  "forms": { "safe_name": { "identifier": "safe_name" } },
  "primaryOutputs": [
    { "path": "BlankPage.xaml" },
    { "path": "BlankPage.xaml.cs" },
    { "path": "BlankPageModel.cs" }
  ]
}
```

- [ ] **Step 2: Write `dotnetcli.host.json`**

`src/Uno.Templates/content/item-mvux-page/.template.config/dotnetcli.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/dotnetcli.host",
  "symbolInfo": {
    "rootNamespace": { "longName": "root-namespace", "shortName": "ns" }
  }
}
```

- [ ] **Step 3: Write `ide.host.json`**

`src/Uno.Templates/content/item-mvux-page/.template.config/ide.host.json`:
```json
{
  "$schema": "http://json.schemastore.org/ide.host",
  "unsupportedHosts": [ { "id": "vs" } ]
}
```

- [ ] **Step 4: Write `BlankPage.xaml`**

`src/Uno.Templates/content/item-mvux-page/BlankPage.xaml`:
```xml
<Page
    x:Class="$rootnamespace$.$safeitemname$"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:$rootnamespace$"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <TextBlock
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Text="{Binding Title}" />
    </Grid>
</Page>
```

- [ ] **Step 5: Write `BlankPage.xaml.cs`**

`src/Uno.Templates/content/item-mvux-page/BlankPage.xaml.cs`:
```csharp
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : Page
{
    public $safeitemname$()
    {
        this.InitializeComponent();
        this.DataContext = new Bindable$safeitemname$Model();
    }
}
```

- [ ] **Step 6: Write `BlankPageModel.cs`** (MVUX record model)

`src/Uno.Templates/content/item-mvux-page/BlankPageModel.cs`:
```csharp
namespace $rootnamespace$;

public partial record $safeitemname$Model
{
    public string Title => "$safeitemname$";
}
```

- [ ] **Step 7: Install and generate (verify file shape)**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\item-mvux-page --force
Remove-Item -Recurse -Force $env:TEMP\itemtest -ErrorAction SilentlyContinue
dotnet new uno-mvux-page -n Feed --root-namespace Acme.App -o $env:TEMP\itemtest
Get-ChildItem $env:TEMP\itemtest
Get-Content $env:TEMP\itemtest\FeedModel.cs
Get-Content $env:TEMP\itemtest\Feed.xaml.cs
```
Expected: `Feed.xaml`, `Feed.xaml.cs`, `FeedModel.cs`; model record `FeedModel` with `Title`; code-behind references `new BindableFeedModel()`.

- [ ] **Step 8: Commit**

```powershell
git add src/Uno.Templates/content/item-mvux-page
git commit -m "feat(templates): add uno-mvux-page item template"
```

---

## Phase 6 — Integration test, docs, CI

### Task 15: Integration smoke test script

Verifies items actually **compile** when added to a real generated app (the generation checks above only verify text output). Requires the Uno workloads to be installed (`dotnet workload install` per the repo prerequisites).

**Files:**
- Create: `build/test-item-templates.ps1`

- [ ] **Step 1: Write the script**

`build/test-item-templates.ps1`:
```powershell
#!/usr/bin/env pwsh
# Generates an Uno app for each presentation framework, adds every item template,
# then builds the app (Desktop head) to prove the items compile.
param(
    [string]$WorkDir = (Join-Path $env:TEMP "uno-item-template-tests")
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$content = Join-Path $repoRoot "src/Uno.Templates/content"

# Install every item template from source.
$items = @(
    "item-page","item-window","item-usercontrol","item-contentdialog",
    "item-resourcedictionary","item-resourcedictionary-codebehind","item-resw",
    "item-templatedcontrol","item-mvvm-page","item-mvux-page"
)
foreach ($i in $items) {
    dotnet new install (Join-Path $content $i) --force
    if ($LASTEXITCODE -ne 0) { throw "Failed to install $i" }
}

function Test-Items {
    param([string]$Name, [string]$Preset, [string]$Presentation, [string]$Markup)

    $appDir = Join-Path $WorkDir $Name
    Remove-Item -Recurse -Force $appDir -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $appDir | Out-Null

    Push-Location $appDir
    try {
        dotnet new unoapp -preset $Preset -presentation $Presentation -markup $Markup -platforms desktop -o . --force
        if ($LASTEXITCODE -ne 0) { throw "unoapp generation failed for $Name" }

        $itemArgs = "-markup $Markup"
        dotnet new uno-page -n SampleItemPage $itemArgs.Split(' ')
        dotnet new uno-window -n SampleItemWindow $itemArgs.Split(' ')
        dotnet new uno-usercontrol -n SampleItemControl $itemArgs.Split(' ')
        dotnet new uno-contentdialog -n SampleItemDialog $itemArgs.Split(' ')
        dotnet new uno-resourcedictionary -n SampleDictionary
        dotnet new uno-resourcedictionary-codebehind -n SampleDictionaryCb
        dotnet new uno-resw -n SampleStrings
        dotnet new uno-templatedcontrol -n SampleTemplatedControl

        if ($Presentation -eq "mvvm") { dotnet new uno-mvvm-page -n SampleMvvmPage }
        if ($Presentation -eq "mvux") { dotnet new uno-mvux-page -n SampleMvuxPage }

        dotnet build -f net10.0-desktop
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $Name" }
        Write-Host "PASS: $Name" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

Test-Items -Name "mvvm-xaml"   -Preset recommended -Presentation mvvm -Markup xaml
Test-Items -Name "mvux-xaml"   -Preset recommended -Presentation mvux -Markup xaml
Test-Items -Name "mvvm-csharp" -Preset recommended -Presentation mvvm -Markup csharp

Write-Host "All item template integration tests passed." -ForegroundColor Green
```

- [ ] **Step 2: Run the integration test locally** (requires Uno workloads)

Run:
```powershell
pwsh ./build/test-item-templates.ps1
```
Expected: `PASS: mvvm-xaml`, `PASS: mvux-xaml`, `PASS: mvvm-csharp`, then "All item template integration tests passed." If the MVUX `Bindable<Model>` constructor differs, fix `item-mvux-page/BlankPage.xaml.cs` now and re-run.

- [ ] **Step 3: Commit**

```powershell
git add build/test-item-templates.ps1
git commit -m "test(templates): add item-template build smoke test"
```

### Task 16: User docs

**Files:**
- Create: `docs/item-templates.md`
- Modify: `README.md`

- [ ] **Step 1: Write `docs/item-templates.md`**

`docs/item-templates.md`:
```markdown
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
```

- [ ] **Step 2: Add a link from `README.md`**

In `README.md`, add this line under the existing intro (exact placement: immediately after the first paragraph):
```markdown

See [docs/item-templates.md](docs/item-templates.md) for the `dotnet new` item templates (Page, Window, UserControl, ContentDialog, ResourceDictionary, .resw, TemplatedControl, MVVM/MVUX pages).
```

- [ ] **Step 3: Commit**

```powershell
git add docs/item-templates.md README.md
git commit -m "docs(templates): document dotnet new item templates"
```

### Task 17: Wire the smoke test into CI

**Files:**
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1: Read the CI workflow to find the right job**

Run:
```powershell
Get-Content .github/workflows/ci.yml
```
Identify the job that runs on `windows-latest` and already has the Uno workloads installed (the same job used by the `run-tests` action / generate-test-matrix). The new step must run after workload installation.

- [ ] **Step 2: Add a step that runs the smoke test**

In that job, after the workload-install step and before/after the existing template tests, add:
```yaml
      - name: Test item templates
        if: runner.os == 'Windows'
        shell: pwsh
        run: pwsh ./build/test-item-templates.ps1
```
(Match the surrounding indentation in `ci.yml`. If the existing matrix job is Linux-only, instead add the step to the Windows job; the smoke test builds the `desktop` head which runs on Windows CI.)

- [ ] **Step 3: Verify the YAML parses**

Run:
```powershell
dotnet tool install -g dotnet-format 2>$null; # no-op if present
# Lightweight YAML sanity check:
pwsh -c "Get-Content .github/workflows/ci.yml | Out-Null; Write-Host 'read ok'"
```
Expected: the file reads without error and the new step is present with correct indentation (confirm visually with `Get-Content`).

- [ ] **Step 4: Commit**

```powershell
git add .github/workflows/ci.yml
git commit -m "ci(templates): run item-template smoke test"
```

---

## Final integration check

- [ ] **Full package build + install** (proves the items ship correctly in the packed `Uno.Templates` nupkg, not just from source folders):

Run:
```powershell
# Uninstall the source-folder installs first so they don't shadow the package.
foreach ($i in "item-page","item-window","item-usercontrol","item-contentdialog","item-resourcedictionary","item-resourcedictionary-codebehind","item-resw","item-templatedcontrol","item-mvvm-page","item-mvux-page") {
    dotnet new uninstall (Resolve-Path ".\src\Uno.Templates\content\$i") 2>$null
}
pwsh ./src/Uno.Templates/reinstall.ps1
dotnet new list uno
```
Expected: `reinstall.ps1` builds and installs the package; `dotnet new list uno` shows all `uno-*` item templates alongside `unoapp`/`unolib`.

- [ ] **Run dotnet format** (per repo convention) on any generated/edited C# in the repo tooling, then confirm clean `git status` of intended files only.

---

## Self-Review (completed by plan author)

- **Spec coverage:** dotnet-new item templates ✅ (Tasks 1–8); modernized code ✅ (file-scoped namespaces throughout); `Themes/Generic.xaml` fix ✅ (Task 8); C# Markup variants ✅ (Tasks 9–12); MVVM/MVUX paired items ✅ (Tasks 13–14); single-source delivery (auto-packaged via existing glob, hidden from VS to avoid duplicating uno.studio items) ✅; docs + CI ✅ (Tasks 15–17).
- **Naming consistency:** shortNames `uno-*`; folders `item-*`; identities `Uno.Platform.Item.*`; `safeItemName`→`$safeitemname$`, `rootNamespace`→`$rootnamespace$` used identically across all tasks. `useCsharpMarkup` computed symbol name identical across Tasks 9–12 and matches the `#if useCsharpMarkup` directive in each code file.
- **sourceName safety:** verified each `sourceName` literal is absent from its own content (`MyUserControl` ∉ `UserControl`; `MyResourceDictionary` ∉ `ResourceDictionary`; resw uses `Strings`+`defaultName Resources` to avoid corrupting `System.Resources.ResX...`).
- **Known residual risk:** MVUX `Bindable<Model>` constructor signature (Task 14) is validated at build time in Task 15; fix-up location is called out explicitly.

---

## Out of scope (future work)

- **Unifying the VS `.vstemplate` items (uno.studio) and these `dotnet new` items into a single source** (WinAppSDK's `templates.props` model). This plan deliberately keeps VS on its existing `.vstemplate` items (via `unsupportedHosts: vs`) to avoid duplicate "Add New Item" entries. A follow-up could retire the uno.studio item templates and have the VS extension consume these `dotnet new` items, eliminating the duplicated content.
- **Title-bar extension + Mica/Acrylic backdrop** Windows-polish options and a **TabView app shape** — separate, lower-priority enhancements identified in the comparison analysis.
```
