# `unoapp` Shell Layouts (NavigationView + TabBar) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `shell-layout` option to the `unoapp` template that scaffolds a menu-based app shell — `navview` (NavigationView side menu) or `tabbar` (bottom tab bar) — that **adapts to the selected navigation model**: region-based (Uno.Extensions.Navigation) when DI + regions are selected, and plain WinUI `Frame` navigation (like the WinAppSDK templates) when not.

**Architecture:** This is a new option *inside* the existing `unoapp` template (not a separate template), so it inherits every other axis (MVVM/MVUX, themes, platforms, auth, etc.) for free and never drifts out of sync. The behavior split reuses `unoapp`'s existing computed symbols `useRegionsNav` / `useFrameNav`:
- **Frame mode** (`useFrameNav`): the **root** `MainPage.xaml` hosts a WinUI `NavigationView`+`Frame` (with `SelectionChanged` code-behind navigating to leaf Pages) or a WinUI `TabView` with inline tab content. No Uno.Extensions dependency. Mirrors WinAppSDK's `navigation-app` / `tabview-app`.
- **Regions mode** (`useRegionsNav`): the `Presentation/MainPage.xaml` hosts a `NavigationView`/`utu:TabBar` as a **navigation region** (`uen:Region.Attached`), with inline content regions switched by `uen:Region.Navigator="Visibility"` and menu items linked via `uen:Region.Name`. Routes are registered as **view-less nested route names** under `Main` (per the documented Uno.Extensions pattern), so there are no extra Page/ViewModel files and no MVVM/MVUX branching.

**Tech Stack:** dotnet new template engine (conditional `#if`/`#elif` content + `cnd:noEmit` markers + `sources` modifiers), WinUI `NavigationView`/`TabView` (frame mode), Uno.Toolkit `utu:TabBar` + Uno.Extensions.Navigation regions (regions mode).

**Scope of this plan:** **XAML markup only.** C# Markup shells are deferred (the C# Markup region-navigation syntax is not documented; see "Out of scope"). When `-markup csharp` is combined with a shell layout, the layout falls back to the existing single MainPage (the shell-layout content is XAML-guarded), and Task 1 records this limitation.

**Key facts established during research (file:line):**
- Navigation mode is driven by `unoapp/.template.config/template.json` computed symbols: `useRegionsNav` (`= useExtensionsNavigation && navigationEvaluator == 'regions'`) and `useFrameNav` (`= useDependencyInjection != 'true' || navigationEvaluator == 'blank'`). The shell-layout symbols combine with these.
- Frame mode root view is `unoapp/MyExtensionsApp.1/MainPage.xaml(.cs)`; `App.blank.cs` does `MainWindow.Content = new Frame()` then `rootFrame.Navigate(typeof(MainPage))`.
- Regions mode root view is `unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml(.cs)`; routes are registered in `App.recommended.cs` `RegisterRoutes(...)`. The `Presentation/**` folder is excluded when `useFrameNav`; the root `MainPage.xaml(.cs)` is excluded when `useExtensionsNavigation && useDependencyInjection`.
- `App.recommended.cs:69-70` already calls `.UseToolkitNavigation()` (gated on `useNavigationToolkit = useExtensionsNavigation && useToolkit`) — *"Add navigation support for toolkit controls such as TabBar and NavigationView"*. **Regions-mode shells therefore require the Toolkit** (on by default in the `recommended` preset).
- Uno.Extensions supports **view-less routes**: `new RouteMap("Main", View: ..., Nested:[ new ("Home"), new ("About") ])` with markup `<utu:TabBarItem uen:Region.Name="Home"/>` and a content `Grid uen:Region.Attached="True" uen:Region.Navigator="Visibility"` holding elements with matching `uen:Region.Name`. (Source: Uno.Extensions "How-To: Define Routes" → RouteMap → View.)
- `$navigationNamespace$` expands to `xmlns:uen="using:Uno.Extensions.Navigation.UI"` in regions mode; `$toolkitNamespace$` to `xmlns:utu="using:Uno.Toolkit.UI"` when toolkit is on; `$themeBackgroundBrush$` / `$toolkitSafeArea$` are theme/toolkit text symbols already used by both MainPage files.

**Local test loop:** Generate from source via `dotnet new install` (or `reinstall.ps1` for the packaged form), then build the `desktop` head. Building a generated `unoapp` requires the Uno workloads installed.

---

## File Structure

**Modified:**
- `src/Uno.Templates/content/unoapp/.template.config/template.json` — add `shellLayout` symbol + 7 computed symbols + `sources` exclude rules.
- `src/Uno.Templates/content/unoapp/.template.config/dotnetcli.host.json` — expose `--shell-layout`.
- `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml` — frame-mode shell content (conditional).
- `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml.cs` — frame-mode NavView `SelectionChanged` + initial navigate (conditional).
- `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml` — regions-mode shell content (conditional).
- `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/App.recommended.cs` — nested view-less routes under `Main` (conditional).
- `.github/actions/ci/generate-test-matrix/action.yml` — new test rows.
- `docs/item-templates.md` (or a new `docs/shell-layouts.md`) + `README.md` — docs.

**Created (frame-mode NavView leaf pages only):**
- `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Pages/HomePage.xaml` + `.xaml.cs`
- `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Pages/AboutPage.xaml` + `.xaml.cs`
- `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Pages/SettingsPage.xaml` + `.xaml.cs`

(Frame-mode TabView uses inline tab content; regions mode uses inline content regions — neither needs leaf files.)

**Phases:**
1. Option + computed symbols + file-exclusion wiring (default `blank` leaves output unchanged).
2. NavView — Frame mode (root MainPage + leaf Pages).
3. TabView — Frame mode (root MainPage inline tabs).
4. NavView — Regions mode (Presentation/MainPage + view-less routes).
5. TabBar — Regions mode (Presentation/MainPage, reuses routes from Phase 4).
6. Integration tests + docs + CI.

---

## Phase 1 — Option, symbols, and wiring

### Task 1: Add the `shellLayout` option and computed symbols

**Files:**
- Modify: `src/Uno.Templates/content/unoapp/.template.config/template.json`
- Modify: `src/Uno.Templates/content/unoapp/.template.config/dotnetcli.host.json`

- [ ] **Step 1: Add the `shellLayout` parameter symbol**

In `template.json`, in the `"symbols"` object, immediately after the existing `"navigation"` symbol block, add:
```json
    "shellLayout": {
      "type": "parameter",
      "datatype": "choice",
      "defaultValue": "blank",
      "displayName": "Shell Layout",
      "description": "Adds a menu-based application shell. 'navview' uses a NavigationView side menu; 'tabbar' uses a tab bar. Uses region-based navigation when DI + regions navigation are enabled, otherwise plain Frame navigation.",
      "choices": [
        { "choice": "blank", "description": "No shell layout (single content page)" },
        { "choice": "navview", "description": "NavigationView (side menu) shell" },
        { "choice": "tabbar", "description": "Tab bar shell" }
      ]
    },
```

- [ ] **Step 2: Add the computed symbols**

In `template.json`, find the existing `"useFrameNav"` computed symbol and add immediately after it:
```json
    "useNavViewLayout": {
      "type": "computed",
      "value": "(shellLayout == 'navview')"
    },
    "useTabBarLayout": {
      "type": "computed",
      "value": "(shellLayout == 'tabbar')"
    },
    "useShellLayout": {
      "type": "computed",
      "value": "(useNavViewLayout || useTabBarLayout)"
    },
    "useNavViewFrame": {
      "type": "computed",
      "value": "(useNavViewLayout && useFrameNav)"
    },
    "useTabViewFrame": {
      "type": "computed",
      "value": "(useTabBarLayout && useFrameNav)"
    },
    "useNavViewRegions": {
      "type": "computed",
      "value": "(useNavViewLayout && useRegionsNav)"
    },
    "useTabBarRegions": {
      "type": "computed",
      "value": "(useTabBarLayout && useRegionsNav)"
    },
```

- [ ] **Step 3: Add file-exclusion rules for the frame-mode leaf pages**

In `template.json`, in the top-level `"sources"` array's first element `"modifiers"` list, add this entry (the leaf Pages exist only for the NavView frame layout):
```json
        {
          "condition": "(!useNavViewFrame)",
          "exclude": [
            "MyExtensionsApp.1/Pages/**"
          ]
        },
```

- [ ] **Step 4: Expose `--shell-layout` in `dotnetcli.host.json`**

In `dotnetcli.host.json`, add to the `"symbolInfo"` object:
```json
    "shellLayout": {
      "longName": "shell-layout",
      "shortName": "shell-layout"
    },
```

- [ ] **Step 5: Verify the option parses and default output is unchanged**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\unoapp --force
dotnet new unoapp --help | Select-String "shell-layout" -Context 0,4
Remove-Item -Recurse -Force $env:TEMP\shelltest -ErrorAction SilentlyContinue
dotnet new unoapp -preset blank -platforms desktop -o $env:TEMP\shelltest
Get-ChildItem $env:TEMP\shelltest\MyApp -ErrorAction SilentlyContinue
```
Expected: `--shell-layout` appears in help with choices `blank`/`navview`/`tabbar` (default `blank`); generating with the default produces the usual blank output (no `Pages/` folder).

> Note for Task body: this plan is XAML-only. C# Markup + shell layout is unsupported for now — the shell content added in later phases is guarded so `-markup csharp` keeps the existing single MainPage. This limitation is intentional and documented in Phase 6.

- [ ] **Step 6: Commit**

```powershell
git add src/Uno.Templates/content/unoapp/.template.config
git commit -m "feat(unoapp): add shell-layout option and computed symbols"
```

---

## Phase 2 — NavigationView, Frame mode (plain WinUI)

Mirrors WinAppSDK `navigation-app`: a `NavigationView` whose `SelectionChanged` navigates an inner `Frame` to leaf Pages, plus a built-in Settings item.

### Task 2: Frame-mode NavView leaf pages

**Files:**
- Create: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Pages/HomePage.xaml` + `.xaml.cs`
- Create: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Pages/AboutPage.xaml` + `.xaml.cs`
- Create: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Pages/SettingsPage.xaml` + `.xaml.cs`

- [ ] **Step 1: Write `Pages/HomePage.xaml`**

`.../MyExtensionsApp.1/Pages/HomePage.xaml`:
```xml
<Page x:Class="MyExtensionsApp._1.Pages.HomePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="{ThemeResource $themeBackgroundBrush$}">
  <Grid>
    <TextBlock Text="Home"
               FontSize="24"
               HorizontalAlignment="Center"
               VerticalAlignment="Center" />
  </Grid>
</Page>
```

- [ ] **Step 2: Write `Pages/HomePage.xaml.cs`**

`.../MyExtensionsApp.1/Pages/HomePage.xaml.cs`:
```csharp
namespace MyExtensionsApp._1.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        this.InitializeComponent();
    }
}
```

- [ ] **Step 3: Write `Pages/AboutPage.xaml`**

`.../MyExtensionsApp.1/Pages/AboutPage.xaml`:
```xml
<Page x:Class="MyExtensionsApp._1.Pages.AboutPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="{ThemeResource $themeBackgroundBrush$}">
  <Grid>
    <TextBlock Text="About"
               FontSize="24"
               HorizontalAlignment="Center"
               VerticalAlignment="Center" />
  </Grid>
</Page>
```

- [ ] **Step 4: Write `Pages/AboutPage.xaml.cs`**

`.../MyExtensionsApp.1/Pages/AboutPage.xaml.cs`:
```csharp
namespace MyExtensionsApp._1.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        this.InitializeComponent();
    }
}
```

- [ ] **Step 5: Write `Pages/SettingsPage.xaml`**

`.../MyExtensionsApp.1/Pages/SettingsPage.xaml`:
```xml
<Page x:Class="MyExtensionsApp._1.Pages.SettingsPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="{ThemeResource $themeBackgroundBrush$}">
  <Grid>
    <TextBlock Text="Settings"
               FontSize="24"
               HorizontalAlignment="Center"
               VerticalAlignment="Center" />
  </Grid>
</Page>
```

- [ ] **Step 6: Write `Pages/SettingsPage.xaml.cs`**

`.../MyExtensionsApp.1/Pages/SettingsPage.xaml.cs`:
```csharp
namespace MyExtensionsApp._1.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
    }
}
```

- [ ] **Step 7: Commit**

```powershell
git add src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Pages
git commit -m "feat(unoapp): add frame-mode NavView leaf pages"
```

### Task 3: NavigationView frame-mode shell in the root MainPage

**Files:**
- Modify: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml`
- Modify: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml.cs`

- [ ] **Step 1: Wrap the root `MainPage.xaml` body with the NavView frame block**

In `.../MyExtensionsApp.1/MainPage.xaml`, replace the `<ScrollViewer>...</ScrollViewer>` element (the entire current body inside `<Page>`) with this conditional block (keep the `<Page ...>` opening tag and its attributes exactly as-is):
```xml
<!--#if (useNavViewFrame)-->
  <NavigationView x:Name="NavView"
                  SelectionChanged="NavView_SelectionChanged"
                  IsBackButtonVisible="Collapsed"
                  IsSettingsVisible="True">
    <NavigationView.MenuItems>
      <NavigationViewItem Content="Home" Tag="Home" />
      <NavigationViewItem Content="About" Tag="About" />
    </NavigationView.MenuItems>
    <Frame x:Name="ContentFrame" />
  </NavigationView>
<!--#else-->
  <ScrollViewer IsTabStop="True">
    <Grid$toolkitSafeArea$>
      <StackPanel
        HorizontalAlignment="Center"
        VerticalAlignment="Center">
        <TextBlock AutomationProperties.AutomationId="HelloTextBlock"
          Text="Hello Uno Platform!"
          HorizontalAlignment="Center" />
<!--#if (mauiEmbedding)-->
<!--#if (useNonMauiPlatforms)-->
        <maui:Grid>
          <embed:MauiHost x:Name="MauiHostElement"
            MaxHeight="500"
            xmlns:embed="using:Uno.Extensions.Maui"
            Source="controls:EmbeddedControl" />
        </maui:Grid>
        <not_maui:Grid>
          <TextBlock AutomationProperties.AutomationId="NotMauiTextBlock"
            Text="Alternative content for Non-Maui targets"
            HorizontalAlignment="Center" />
        </not_maui:Grid>
<!--#else-->
        <embed:MauiHost x:Name="MauiHostElement"
          MaxHeight="500"
          xmlns:embed="using:Uno.Extensions.Maui"
          Source="controls:EmbeddedControl" />
<!--#endif-->
<!--#endif-->
      </StackPanel>
    </Grid>
  </ScrollViewer>
<!--#endif-->
```

- [ ] **Step 2: Add `SelectionChanged` handling and initial navigation in `MainPage.xaml.cs`**

In `.../MyExtensionsApp.1/MainPage.xaml.cs`, replace the `#else` branch of the constructor (`this.InitializeComponent();`) and add the handler so the file reads:
```csharp
//-:cnd:noEmit
namespace MyExtensionsApp._1;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this
#if useMaterial
            .Background(Theme.Brushes.Background.Default)
#else
            .Background(ThemeResource.Get<Brush>("$themeBackgroundBrush$"))
#endif
            .Content(new StackPanel()
            .VerticalAlignment(VerticalAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                new TextBlock()
                    .Text("Hello Uno Platform!")
            ));
#else
        this.InitializeComponent();
#if (useNavViewFrame)
        ContentFrame.Navigate(typeof(Pages.HomePage));
        NavView.SelectedItem = NavView.MenuItems[0];
#endif
#endif
//-:cnd:noEmit
    }
//+:cnd:noEmit
#if (useNavViewFrame)

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(Pages.SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem { Tag: string tag })
        {
            switch (tag)
            {
                case "Home":
                    ContentFrame.Navigate(typeof(Pages.HomePage));
                    break;
                case "About":
                    ContentFrame.Navigate(typeof(Pages.AboutPage));
                    break;
            }
        }
    }
#endif
//-:cnd:noEmit
}
```

- [ ] **Step 3: Generate (blank preset, frame nav) and inspect**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\unoapp --force
Remove-Item -Recurse -Force $env:TEMP\shelltest -ErrorAction SilentlyContinue
dotnet new unoapp -preset blank -platforms desktop --shell-layout navview -o $env:TEMP\shelltest
Get-ChildItem -Recurse $env:TEMP\shelltest\MyApp\Pages
Get-Content $env:TEMP\shelltest\MyApp\MainPage.xaml
Get-Content $env:TEMP\shelltest\MyApp\MainPage.xaml.cs
```
Expected: `Pages\HomePage/AboutPage/SettingsPage` generated; `MainPage.xaml` shows the `NavigationView` block (no `#if` leftovers); `MainPage.xaml.cs` has `NavView_SelectionChanged` and the initial navigate.

- [ ] **Step 4: Build the generated app (proves it compiles)**

Run:
```powershell
Push-Location $env:TEMP\shelltest\MyApp
dotnet build -f net10.0-desktop
Pop-Location
```
Expected: build succeeds. If `net10.0-desktop` TFM differs, use the TFM shown in the generated `.csproj`.

- [ ] **Step 5: Verify `blank` layout still produces the Hello page**

Run:
```powershell
Remove-Item -Recurse -Force $env:TEMP\shelltest2 -ErrorAction SilentlyContinue
dotnet new unoapp -preset blank -platforms desktop -o $env:TEMP\shelltest2
Get-Content $env:TEMP\shelltest2\MyApp\MainPage.xaml | Select-String "Hello Uno Platform"
```
Expected: the `Hello Uno Platform!` `TextBlock` is present and no `Pages/` folder exists.

- [ ] **Step 6: Commit**

```powershell
git add src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml.cs
git commit -m "feat(unoapp): NavigationView frame-mode shell layout"
```

---

## Phase 3 — TabView, Frame mode (plain WinUI)

A WinUI `TabView` with static tabs hosting inline content. (WinAppSDK's dynamic add/close + custom title-bar drag region is Windows-only; a static, cross-platform TabView is the right starter for Uno.)

### Task 4: TabView frame-mode shell in the root MainPage

**Files:**
- Modify: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml`

- [ ] **Step 1: Add the `useTabViewFrame` branch to `MainPage.xaml`**

In `.../MyExtensionsApp.1/MainPage.xaml`, change the opening of the conditional block from `<!--#if (useNavViewFrame)-->` to add a TabView branch, so the structure becomes `#if useNavViewFrame ... #elif useTabViewFrame ... #else ... #endif`. Insert this new branch **between** the NavView block's `</NavigationView>` and the existing `<!--#else-->`:
```xml
<!--#elif (useTabViewFrame)-->
  <TabView>
    <TabViewItem Header="Home" IsClosable="False">
      <Grid>
        <TextBlock Text="Home"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
      </Grid>
    </TabViewItem>
    <TabViewItem Header="About" IsClosable="False">
      <Grid>
        <TextBlock Text="About"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
      </Grid>
    </TabViewItem>
  </TabView>
```
(The `<!--#else-->` Hello block and `<!--#endif-->` remain unchanged after this branch.)

- [ ] **Step 2: Generate (blank preset) + build**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\unoapp --force
Remove-Item -Recurse -Force $env:TEMP\shelltest -ErrorAction SilentlyContinue
dotnet new unoapp -preset blank -platforms desktop --shell-layout tabbar -o $env:TEMP\shelltest
Get-Content $env:TEMP\shelltest\MyApp\MainPage.xaml
Push-Location $env:TEMP\shelltest\MyApp
dotnet build -f net10.0-desktop
Pop-Location
```
Expected: `MainPage.xaml` shows the `TabView` with two `TabViewItem`s (no `#if` leftovers, no `Pages/` folder), and the app builds.

- [ ] **Step 3: Commit**

```powershell
git add src/Uno.Templates/content/unoapp/MyExtensionsApp.1/MainPage.xaml
git commit -m "feat(unoapp): TabView frame-mode shell layout"
```

---

## Phase 4 — NavigationView, Regions mode (Uno.Extensions.Navigation)

Region-based: `NavigationView` as a region, inline content regions switched by visibility, view-less nested routes under `Main`. Requires DI + regions + Toolkit (the `recommended` preset).

### Task 5: Register view-less nested routes for the shell

**Files:**
- Modify: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/App.recommended.cs`

- [ ] **Step 1: Add nested `Home`/`About` route names under the `Main` route**

In `.../MyExtensionsApp.1/App.recommended.cs`, in `RegisterRoutes`, locate the `#if (shell)` route registration where `Main` is registered nested under the empty Shell route, and the `#else` branch where `Main` is a top-level `IsDefault` route. Add a `Nested` argument to **both** `Main` `RouteMap`s, guarded for shell layouts.

For the `#if (shell)` branch, change the `Main` entry to:
```csharp
                new ("Main", View: views.FindByViewModel<$mainRouteViewModel$>(), IsDefault:true
#if (useNavViewRegions || useTabBarRegions)
                    , Nested:
                    [
                        new ("Home", IsDefault:true),
                        new ("About")
                    ]
#endif
                ),
```

For the `#else` branch, change the `Main` entry to:
```csharp
        new RouteMap("Main", View: views.FindByViewModel<$mainRouteViewModel$>(), IsDefault:true
#if (useNavViewRegions || useTabBarRegions)
            , Nested:
            [
                new ("Home", IsDefault:true),
                new ("About")
            ]
#endif
        )
```
(Preserve the surrounding `#if (useSampleContent)` `Second` route entries exactly as they are.)

- [ ] **Step 2: Commit**

```powershell
git add src/Uno.Templates/content/unoapp/MyExtensionsApp.1/App.recommended.cs
git commit -m "feat(unoapp): register view-less shell routes for regions navigation"
```

### Task 6: NavigationView regions shell in Presentation/MainPage

**Files:**
- Modify: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml`

- [ ] **Step 1: Add the NavView regions branch as the first content branch**

In `.../MyExtensionsApp.1/Presentation/MainPage.xaml`, the body currently starts with `<!--#if (useSampleContent)-->`. Wrap it so the shell layout takes precedence. Replace the opening `<!--#if (useSampleContent)-->` with:
```xml
<!--#if (useNavViewRegions)-->
  <Grid uen:Region.Attached="True">
    <NavigationView uen:Region.Attached="True"
                    PaneDisplayMode="Left"
                    IsBackButtonVisible="Collapsed"
                    IsSettingsVisible="False">
      <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" uen:Region.Name="Home" />
        <NavigationViewItem Content="About" uen:Region.Name="About" />
      </NavigationView.MenuItems>
      <Grid uen:Region.Attached="True"
            uen:Region.Navigator="Visibility">
        <Grid uen:Region.Name="Home" Visibility="Collapsed">
          <TextBlock Text="Home"
                     FontSize="24"
                     HorizontalAlignment="Center"
                     VerticalAlignment="Center" />
        </Grid>
        <Grid uen:Region.Name="About" Visibility="Collapsed">
          <TextBlock Text="About"
                     FontSize="24"
                     HorizontalAlignment="Center"
                     VerticalAlignment="Center" />
        </Grid>
      </Grid>
    </NavigationView>
  </Grid>
<!--#elif (useSampleContent)-->
```
(Everything from the original `<ScrollViewer>` through the final `<!--#endif-->` is preserved; you are only changing the first `#if` into `#elif` and prepending the new `#if (useNavViewRegions)` branch.)

- [ ] **Step 2: Generate (recommended preset) + inspect**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\unoapp --force
Remove-Item -Recurse -Force $env:TEMP\shelltest -ErrorAction SilentlyContinue
dotnet new unoapp -preset recommended -platforms desktop --shell-layout navview -sample false -o $env:TEMP\shelltest
Get-Content $env:TEMP\shelltest\MyApp\Presentation\MainPage.xaml
Get-Content $env:TEMP\shelltest\MyApp\App.cs | Select-String -Pattern "Home|About|Main" -Context 1,1
```
Expected: `Presentation\MainPage.xaml` shows the `NavigationView` with `uen:Region.Attached` and named content regions (no `#if` leftovers, `uen:` namespace present); `App.cs` `RegisterRoutes` has `Main` with nested `Home`(IsDefault)/`About`.

- [ ] **Step 3: Build the generated app**

Run:
```powershell
Push-Location $env:TEMP\shelltest\MyApp
dotnet build -f net10.0-desktop
Pop-Location
```
Expected: build succeeds. **If it fails on the region wiring**, reconcile against the documented pattern in Uno.Extensions "Navigate Between Menu Items using NavigationView" (Region.Attached on the outer Grid + NavigationView, Region.Navigator="Visibility" on the content Grid, Region.Name on items and content) and re-run. Do not proceed until green.

- [ ] **Step 4: Commit**

```powershell
git add src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml
git commit -m "feat(unoapp): NavigationView regions-mode shell layout"
```

---

## Phase 5 — TabBar, Regions mode (Uno.Toolkit + regions)

Reuses the nested routes from Task 5. Uses `utu:TabBar` as a bottom region. Requires Toolkit (recommended preset).

### Task 7: TabBar regions shell in Presentation/MainPage

**Files:**
- Modify: `src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml`

- [ ] **Step 1: Add the TabBar regions branch**

In `.../MyExtensionsApp.1/Presentation/MainPage.xaml`, add a `useTabBarRegions` branch between the `useNavViewRegions` branch's closing `</Grid>` and the `<!--#elif (useSampleContent)-->` line, so the chain is `#if useNavViewRegions ... #elif useTabBarRegions ... #elif useSampleContent ... #else ... #endif`. Insert:
```xml
<!--#elif (useTabBarRegions)-->
  <Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
      <RowDefinition />
      <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
      <Grid uen:Region.Name="Home" Visibility="Collapsed">
        <TextBlock Text="Home"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
      </Grid>
      <Grid uen:Region.Name="About" Visibility="Collapsed">
        <TextBlock Text="About"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
      </Grid>
    </Grid>
    <utu:TabBar Grid.Row="1"
                VerticalAlignment="Bottom"
                uen:Region.Attached="True">
      <utu:TabBar.Items>
        <utu:TabBarItem Content="Home" uen:Region.Name="Home" IsSelectable="True" />
        <utu:TabBarItem Content="About" uen:Region.Name="About" IsSelectable="True" />
      </utu:TabBar.Items>
    </utu:TabBar>
  </Grid>
```

- [ ] **Step 2: Generate (recommended preset) + build**

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\unoapp --force
Remove-Item -Recurse -Force $env:TEMP\shelltest -ErrorAction SilentlyContinue
dotnet new unoapp -preset recommended -platforms desktop --shell-layout tabbar -sample false -o $env:TEMP\shelltest
Get-Content $env:TEMP\shelltest\MyApp\Presentation\MainPage.xaml
Push-Location $env:TEMP\shelltest\MyApp
dotnet build -f net10.0-desktop
Pop-Location
```
Expected: `Presentation\MainPage.xaml` shows the `utu:TabBar` region block (no `#if` leftovers); the app builds. **If the TabBar region fails**, confirm `.UseToolkitNavigation()` is present in the generated `App.cs` (it should be, via `useNavigationToolkit`) and reconcile against the Uno.Extensions "Navigate Between Tab Views using TabBar" pattern (root Grid `uen:Region.Attached="True"`, content Grid `Region.Navigator="Visibility"`, items with `uen:Region.Name`). Do not proceed until green.

- [ ] **Step 3: Commit**

```powershell
git add src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Presentation/MainPage.xaml
git commit -m "feat(unoapp): TabBar regions-mode shell layout"
```

---

## Phase 6 — CI, docs, and the wizard

### Task 8: Add CI test matrix rows

**Files:**
- Modify: `.github/actions/ci/generate-test-matrix/action.yml`

- [ ] **Step 1: Add four rows to the `$tests` array**

In `.github/actions/ci/generate-test-matrix/action.yml`, in the `$tests = @(...)` list (format `"TestName;[PlatformFilter];TemplateName;Arguments"`), add:
```
            "NavViewFrame;!macOS;unoapp;-skip -preset blank --shell-layout navview",
            "TabViewFrame;!macOS;unoapp;-skip -preset blank --shell-layout tabbar",
            "NavViewRegions;!macOS;unoapp;-skip -preset recommended --shell-layout navview -sample false",
            "TabBarRegions;!macOS;unoapp;-skip -preset recommended --shell-layout tabbar -sample false",
```
(Match the surrounding indentation and trailing-comma style.)

- [ ] **Step 2: Verify the YAML still reads**

Run:
```powershell
Get-Content .github/actions/ci/generate-test-matrix/action.yml | Select-String "shell-layout"
```
Expected: the four new rows are present.

- [ ] **Step 3: Commit**

```powershell
git add .github/actions/ci/generate-test-matrix/action.yml
git commit -m "ci(unoapp): test shell-layout combinations"
```

### Task 9: Documentation

**Files:**
- Create: `docs/shell-layouts.md`
- Modify: `README.md`

- [ ] **Step 1: Write `docs/shell-layouts.md`**

`docs/shell-layouts.md`:
```markdown
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
```

- [ ] **Step 2: Link from `README.md`**

In `README.md`, after the first paragraph, add:
```markdown

See [docs/shell-layouts.md](docs/shell-layouts.md) for the `--shell-layout` option (NavigationView / TabBar app shells).
```

- [ ] **Step 3: Commit**

```powershell
git add docs/shell-layouts.md README.md
git commit -m "docs(unoapp): document the shell-layout option"
```

### Task 10: Surface the option in the IDE wizard metadata (if applicable)

**Files:**
- Modify (only if the option must appear in the Uno Studio wizard UI): `src/Uno.Templates/content/unoapp/.template.config/TemplateWizard.json`

- [ ] **Step 1: Read `TemplateWizard.json` to see how options are surfaced**

Run:
```powershell
Get-Content src/Uno.Templates/content/unoapp/.template.config/TemplateWizard.json | Select-String -Pattern "navigation|shell|presentation" -Context 2,2
```
Determine whether the wizard enumerates options explicitly (if so, add a `shellLayout` entry mirroring the existing `navigation` entry's shape) or reads them from `template.json` automatically (if so, no change needed).

- [ ] **Step 2: Add the wizard entry if required**

If the wizard lists options explicitly, add a `shellLayout` group/section mirroring the `navigation` entry's structure (label "Shell Layout", the three choices, default `blank`). If the wizard is data-driven from `template.json`, record "no change required" and skip.

- [ ] **Step 3: Commit (if changed)**

```powershell
git add src/Uno.Templates/content/unoapp/.template.config/TemplateWizard.json
git commit -m "feat(unoapp): surface shell-layout in the template wizard"
```

---

## Final integration check

- [ ] **Build all four combinations + the unchanged defaults** (requires Uno workloads):

Run:
```powershell
dotnet new install .\src\Uno.Templates\content\unoapp --force
$cases = @(
  @{ n="navview-frame";  args="-preset blank -platforms desktop --shell-layout navview" },
  @{ n="tabbar-frame";   args="-preset blank -platforms desktop --shell-layout tabbar" },
  @{ n="navview-regions"; args="-preset recommended -platforms desktop --shell-layout navview -sample false" },
  @{ n="tabbar-regions";  args="-preset recommended -platforms desktop --shell-layout tabbar -sample false" },
  @{ n="default-blank";  args="-preset blank -platforms desktop" },
  @{ n="default-recommended"; args="-preset recommended -platforms desktop" }
)
foreach ($c in $cases) {
  $dir = Join-Path $env:TEMP "shell-$($c.n)"
  Remove-Item -Recurse -Force $dir -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Force $dir | Out-Null
  Push-Location $dir
  iex "dotnet new unoapp $($c.args) -o ."
  dotnet build -f net10.0-desktop
  if ($LASTEXITCODE -ne 0) { Write-Error "FAILED: $($c.n)"; Pop-Location; break }
  Write-Host "PASS: $($c.n)" -ForegroundColor Green
  Pop-Location
}
```
Expected: all six print `PASS`. The two `default-*` cases confirm the option does not regress existing output.

- [ ] **Run `dotnet format`** on `App.recommended.cs` / `MainPage.xaml.cs` edits (per repo convention) and confirm only intended files are staged.

---

## Self-Review (completed by plan author)

- **Spec coverage:** NavigationView shape ✅ (frame: Tasks 2–3; regions: Task 6); TabView/TabBar shape ✅ (frame: Task 4; regions: Task 7); "different behavior for Uno.Navigation extensions vs plain setup" ✅ — driven by reusing `useRegionsNav`/`useFrameNav` (Task 1) so frame mode produces plain WinUI `NavigationView`/`TabView`+`Frame` and regions mode produces region-wired controls; delivered as a `unoapp` option (not a duplicate template) ✅; CI + docs ✅ (Tasks 8–10).
- **Symbol consistency:** `useNavViewLayout`/`useTabBarLayout`/`useShellLayout`/`useNavViewFrame`/`useTabViewFrame`/`useNavViewRegions`/`useTabBarRegions` defined once (Task 1) and used with identical names in every `#if` (Tasks 3,4,5,6,7). Route names `Home`/`About` match between `App.recommended.cs` (Task 5) and the `uen:Region.Name` values in both regions MainPage branches (Tasks 6,7).
- **Risk controls:** the regions route registration uses the documented **view-less** form (no leaf files / no ViewMaps / no MVVM-MVUX branching), the lowest-risk option. Each regions phase ends with a mandatory `dotnet build` gate and a named doc pattern to reconcile against if wiring fails.
- **Known constraints (documented):** regions-mode shells require the Toolkit (recommended preset); shell layouts are XAML-only for now.

---

## Out of scope (follow-up plans)

- **C# Markup shell layouts.** The C# Markup region-navigation syntax (`uen:` equivalents as fluent extensions) is not documented; needs a research spike before implementation. Until then `-markup csharp` + `--shell-layout` falls back to the single page (Task 1).
- **Polish:** NavigationView built-in Settings page in regions mode; responsive TabBar (horizontal on narrow, vertical/`utu:Responsive` on wide, per the Uno Chefs NavigationShell recipe); Material `TabBarItem` styles + icons; per-route **views** (dedicated Pages + ViewMaps) instead of inline content regions, for apps that want richer menu pages.
- **WinAppSDK TabView parity:** dynamic add/close tabs + custom title-bar drag region (Windows-only) — a separate, Windows-targeted enhancement.
```
