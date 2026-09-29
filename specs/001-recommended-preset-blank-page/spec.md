# Spec: Simplify Recommended Preset to a Blank Single Page

## Summary

Change the **Recommended** preset so that it generates a project with a **single blank page** (similar to the Blank preset's "Hello Uno Platform!" page), while retaining **all Uno platform features** fully configured in `App.xaml.cs`. The Recommended preset should remain the opinionated, batteries-included starting point — but with a blank canvas instead of a multi-page sample app.

## Motivation

New users selecting the Recommended preset currently receive a multi-page sample app (Shell → MainPage → SecondPage) with data binding, navigation commands, and Entity models. While this demonstrates framework capabilities, it also forces developers to immediately tear down sample scaffolding before they can start building their own app. A blank page with all features pre-configured lets developers start from a clean slate while still having the full Uno platform stack ready to use.

## Current State

### Recommended Preset (today)

| Feature | Value |
|---------|-------|
| Navigation | Regions (`Shell` → `MainPage` → `SecondPage`) |
| Architecture | MVUX (default) or MVVM |
| Theme | Material |
| DI | ✅ Enabled |
| Logging | ✅ Default |
| Configuration | ✅ Enabled |
| Localization | ✅ Enabled |
| HTTP | Kiota |
| Toolkit | ✅ Enabled |
| Theme Service | ✅ Enabled |
| DSP Generator | ✅ Enabled |
| Simple Theme | ❌ Disabled |

**Files generated (Presentation layer):**
- `Shell.xaml` / `Shell.xaml.cs` — Root navigation container with `ExtendedSplashScreen`
- `ShellModel.cs` / `ShellViewModel.cs` — Shell route ViewModel
- `MainPage.xaml` / `MainPage.xaml.cs` — Feature-rich page with TextBox, "Go to Second Page" button, Logout button
- `MainModel.cs` / `MainViewModel.cs` — MainPage ViewModel with `GoToSecond` command, `Name` state, `Title`, navigation
- `SecondPage.xaml` / `SecondPage.xaml.cs` — Secondary page displaying `Entity.Name`
- `SecondModel.cs` / `SecondViewModel.cs` — SecondPage ViewModel receiving `Entity` data
- `LoginPage.xaml` / `LoginPage.xaml.cs` (conditional on authentication)
- `LoginModel.cs` / `LoginViewModel.cs` (conditional on authentication)
- `Models/Entity.cs` — Data record used for navigation data passing

**Route registration in `App.recommended.cs`:**
```csharp
views.Register(
    new ViewMap(ViewModel: typeof($shellRouteViewModel$)),
    new ViewMap<MainPage, $mainRouteViewModel$>(),
    new DataViewMap<SecondPage, $secondRouteViewModel$, Entity>()
);
routes.Register(
    new RouteMap("", View: views.FindByViewModel<$shellRouteViewModel$>(),
        Nested: [
            new ("Main", View: views.FindByViewModel<$mainRouteViewModel$>(), IsDefault: true),
            new ("Second", View: views.FindByViewModel<$secondRouteViewModel$>()),
        ])
);
```

### Blank Preset (today, for comparison)

| Feature | Value |
|---------|-------|
| Navigation | Frame-based (no Shell, no regions) |
| Architecture | None |
| Theme | Fluent |
| DI | ❌ Disabled |
| Everything else | ❌ Disabled |

**Files generated:**
- `MainPage.xaml` — Simple "Hello Uno Platform!" page
- `MainPage.xaml.cs` — Minimal code-behind
- `App.blank.cs` (renamed to `App.xaml.cs`) — Synchronous Frame-based navigation

---

## Proposed State

### Recommended Preset (after this change)

All infrastructure features remain **identical** to today's Recommended preset:

| Feature | Value | Changed? |
|---------|-------|----------|
| Navigation | Regions (Shell wrapping a single MainPage) | ⚠️ Simplified |
| Architecture | MVUX (default) or MVVM | No change |
| Theme | **Simple** (via `SimpleTheme` UnoFeature) | ✅ Changed |
| DI | ✅ Enabled | No change |
| Logging | ✅ Default | No change |
| Configuration | ✅ Enabled | No change |
| Localization | ✅ Enabled | No change |
| HTTP | Kiota | No change |
| Toolkit | ✅ Enabled | No change |
| Theme Service | ✅ Enabled | No change |
| DSP Generator | ❌ Disabled (Material only) | ✅ Changed |

### UI/Page changes

The Recommended preset will generate:

1. **Shell.xaml / Shell.xaml.cs** — Kept as-is (root navigation container with `ExtendedSplashScreen`)
2. **ShellModel.cs / ShellViewModel.cs** — Kept (required for Shell route registration)
3. **MainPage.xaml** — **Replaced** with a blank page (similar to Blank preset's MainPage but inside the Presentation folder, compatible with regions navigation)
4. **MainPage.xaml.cs** — **Simplified** to minimal code-behind (ViewModel binding retained)
5. **MainModel.cs / MainViewModel.cs** — **Simplified** (remove `GoToSecond` command, `Name` state; keep minimal structure)
6. **SecondPage.\*** — **Removed** from default Recommended output
7. **SecondModel.cs / SecondViewModel.cs** — **Removed** from default Recommended output
8. **Models/Entity.cs** — **Removed** (no longer needed without SecondPage data passing)
9. **LoginPage.\* / LoginModel.cs / LoginViewModel.cs** — Unchanged (still conditional on authentication)

### What the new blank MainPage looks like

The `Presentation/MainPage.xaml` should be simplified to:

```xml
<Page x:Class="MyExtensionsApp._1.Presentation.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      ...
      NavigationCacheMode="Required"
      Background="{ThemeResource $themeBackgroundBrush$}">
  <Grid>
    <TextBlock Text="Hello Uno Platform!"
               HorizontalAlignment="Center"
               VerticalAlignment="Center" />
  </Grid>
</Page>
```

The C# Markup equivalent should also be simplified to match.

### What the route registration looks like

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof($shellRouteViewModel$)),
#if (useAuthentication)
        new ViewMap<LoginPage, $loginRouteViewModel$>(),
#endif
        new ViewMap<MainPage, $mainRouteViewModel$>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<$shellRouteViewModel$>(),
            Nested:
            [
#if (useAuthentication)
                new ("Login", View: views.FindByViewModel<$loginRouteViewModel$>()),
#endif
                new ("Main", View: views.FindByViewModel<$mainRouteViewModel$>(), IsDefault: true),
            ]
        )
    );
}
```

Key changes:
- `MainPage` view mapping and route kept with ViewModel (MVUX/MVVM retained)
- `SecondPage` route, view mapping, and `DataViewMap<SecondPage, ..., Entity>` removed entirely
- No changes to how `MainPage` is registered — it still uses `ViewMap<MainPage, $mainRouteViewModel$>`

---

## Detailed File Changes

### 1. `TemplateWizard.json` — Preset definition

**File:** `.template.config/TemplateWizard.json` (lines 119–165)

Change the Recommended preset's `theme` parameter from `"material"` to `"simple"`:

```json
{
    "Id": "recommended",
    ...
    "Parameters": {
        ...
        "theme": "simple",    // Changed from "material"
        ...
    }
}
```

Also update the `Features` display list from `["MVUX", "Material", ...]` to `["MVUX", "Simple", ...]`.

> **Note:** Users can still explicitly choose `-presentation mvvm` or `-presentation none` with the Recommended preset via CLI or Custom preset in the wizard.

### 1a. `template.json` — Preset theme default

**File:** `.template.config/template.json`

Update the `presetThemeDefault` (or equivalent switch) for the recommended case to use `"simple"` instead of `"material"`. Ensure the `SimpleTheme` UnoFeature is included when `theme == "simple"`.

### 2. `template.json` — Preset architecture default

**File:** `.template.config/template.json` (lines 1095–1111)

No change needed. The `presetArchitectureDefault` for the recommended case remains `"mvux"`.

### 3. `template.json` — SecondPage and Entity exclusion

**File:** `.template.config/template.json`

Since the Recommended preset still uses MVUX by default, the existing architecture-based inclusion logic for ViewModels remains valid. However, `SecondPage` and `Entity` files must be excluded unconditionally from the default Recommended output.

Add a new exclusion rule so that SecondPage and Entity are excluded unless a hypothetical future flag re-enables them (or remove them from the template entirely):

```json
{
    "condition": "true",
    "exclude": [
        "MyExtensionsApp.1/Presentation/SecondPage.xaml",
        "MyExtensionsApp.1/Presentation/SecondPage.xaml.cs",
        "MyExtensionsApp.1/Presentation/SecondModel.cs",
        "MyExtensionsApp.1/Presentation/SecondViewModel.cs",
        "MyExtensionsApp.1/Models/Entity.cs"
    ]
}
```

> **Alternative approach:** Rather than conditional exclusion, simply delete the SecondPage, SecondModel, SecondViewModel, and Entity template files entirely, and remove all references to them in the template. This is cleaner since no preset or option will generate them after this change.

### 4. `App.recommended.cs` — Route registration

**File:** `MyExtensionsApp.1/App.recommended.cs` (lines 285–313)

Remove the SecondPage route registration. The `MainPage` registration with its ViewModel remains:

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
#if (useRegionsNav)
    views.Register(
        new ViewMap(ViewModel: typeof($shellRouteViewModel$)),
#if (useAuthentication)
        new ViewMap<LoginPage, $loginRouteViewModel$>(),
#endif
#if (useMvux || useMvvm)
        new ViewMap<MainPage, $mainRouteViewModel$>()
#else
        new ViewMap<MainPage>()
#endif
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<$shellRouteViewModel$>(),
            Nested:
            [
#if (useAuthentication)
                new ("Login", View: views.FindByViewModel<$loginRouteViewModel$>()),
#endif
#if (useMvux || useMvvm)
                new ("Main", View: views.FindByViewModel<$mainRouteViewModel$>(), IsDefault:true),
#else
                new ("Main", View: views.FindByView<MainPage>(), IsDefault:true),
#endif
            ]
        )
    );
#endif
}
```

Key changes vs. today:
- Removed `DataViewMap<SecondPage, $secondRouteViewModel$, Entity>()` view registration
- Removed `new ("Second", View: views.FindByViewModel<$secondRouteViewModel$>())` route
- All `#if (useMvux || useMvvm)` / `#else` branching for `MainPage` remains to support the `-presentation none` CLI override

> **Note:** The `useMvvm` computed value does **not** need to change. Since the default architecture is still `"mvux"`, the existing computed value `"architectureEvaluator == 'mvvm' || (architectureEvaluator == 'none' && useExtensionsNavigation)"` only activates for explicit `-presentation none` with extensions navigation, which is acceptable existing behavior.

### 5. `Presentation/MainPage.xaml` — Simplified content

**File:** `MyExtensionsApp.1/Presentation/MainPage.xaml`

Replace the current feature-rich page content with a blank page. The page should:
- Keep the `NavigationCacheMode="Required"` attribute
- Keep the toolkit `SafeArea` support
- Remove the TextBox, "Go to Second Page" button, Logout button
- Show a simple centered "Hello Uno Platform!" text
- Keep MAUI embedding conditional content (if applicable)
- Keep the ViewModel `DataContext` binding (MVUX is still the default)

### 6. `Presentation/MainPage.xaml.cs` — Simplified code-behind

**File:** `MyExtensionsApp.1/Presentation/MainPage.xaml.cs`

Keep the `DataContext<MainViewModel>` binding since MVUX remains the default. Simplify by removing any event handlers related to the old multi-page sample content.

### 6a. `Presentation/MainModel.cs` / `Presentation/MainViewModel.cs` — Simplified ViewModel

**File:** `MyExtensionsApp.1/Presentation/MainModel.cs` (MVUX) and `MyExtensionsApp.1/Presentation/MainViewModel.cs` (MVVM)

Simplify the existing ViewModel to remove:
- `GoToSecond` command and navigation logic
- `Name` state / `Title` property
- Any `Entity`-related references
- `INavigator` dependency (no longer needed without SecondPage navigation)

The resulting ViewModel should be a minimal empty shell ready for the developer to add their own properties and commands.

### 7. `template.json` — `secondRouteViewModel` symbol

**File:** `.template.config/template.json` (lines 2097–2114)

The `secondRouteViewModel` generated symbol should be removed (or left as dead code) since `SecondPage` no longer exists in any preset. If kept, it will simply not be referenced anywhere.

### 8. `template.json` — `useMvvm` computed value

**File:** `.template.config/template.json` (line 1129)

No change needed. Since the Recommended preset retains `architecture == 'mvux'` by default, the existing `useMvvm` computed value does not interfere:

```json
"value": "architectureEvaluator == 'mvvm' || (architectureEvaluator == 'none' && useExtensionsNavigation)"
```

This value only activates when a user explicitly sets `-presentation mvvm` or `-presentation none` with extensions navigation, both of which are acceptable edge-case behaviors.

---

## CI Test Scenarios

### Existing scenarios that validate related configurations

These existing tests should continue to pass after the changes:

| Test Name | Arguments | What it validates |
|-----------|-----------|-------------------|
| `Recommended` | `-skip -preset recommended` | **Primary test** — will now validate the new blank page Recommended preset |
| `RecommendedMarkup` | `-skip -preset recommended -markup csharp` | C# Markup variant of new Recommended |
| `RecommendedMarkupDsp` | `-skip -preset recommended -markup csharp -dsp` | C# Markup + DSP variant |
| `RecommendedNoPresentation` | `-preset recommended -presentation none` | Already tests recommended with no presentation |
| `BlankDINavNoPresentation` | `-presentation none -di -nav regions -config` | Tests blank + DI + regions + no presentation |
| `FrameNavigation` | `-skip -preset recommended --navigation blank` | Recommended with frame nav |
| `NativeRecommended` | `-skip --renderer native -preset=recommended` | Native renderer variant |

### New test scenarios to add

Add these new entries to `.github/actions/ci/generate-test-matrix/action.yml`:

```powershell
# Validate that explicitly opting into no presentation with recommended preset
# generates a blank page with no ViewModels
"RecommendedNoPresentation;;unoapp;-skip -preset recommended -presentation none",

# Validate recommended with no presentation + auth combinations
"RecommendedNoPresentationCustomAuth;!macOS;unoapp;-skip -preset recommended -presentation none -auth custom",
```

### Existing tests that need review

These tests already exist and should continue to pass since the default architecture (MVUX) is unchanged:

| Test Name | Arguments | Impact |
|-----------|-----------|--------|
| `Recommended` | `-skip -preset recommended` | Still uses MVUX; will now validate the simplified blank page (no SecondPage) |
| `RecommendedMarkup` | `-skip -preset recommended -markup csharp` | Same — C# Markup variant of simplified Recommended |
| `RecommendedNoPresentation` | `-preset recommended -presentation none` | Explicit no-presentation override; verify blank page with no ViewModels |
| `BlankDINavNoPresentation` | `-presentation none -di -nav regions -config` | Unaffected — no changes to `useMvvm` computed value |

---

## Migration / Backward Compatibility

- **CLI users** who run `dotnet new unoapp -preset recommended` will get a simplified blank page with MVUX still configured. The project structure is simpler (no SecondPage/Entity), but all features including MVUX are present.
- **Visual Studio wizard** users selecting the Recommended preset will get the new blank page with MVUX. The Custom preset allows further customization.
- **Existing projects** are not affected — this only changes what new projects generate.
- **Documentation** should be updated to reflect that the Recommended preset now generates a blank page instead of a multi-page sample app.

## Open Questions

1. **Should the TemplateWizard.json `Features` list be updated?** Currently the Recommended preset lists `["MVUX", "Material", "Configuration", "Localization", "Testing"]`. "Material" should be replaced with "Simple" (or the appropriate display name for the Simple Theme) to reflect the new default.
2. **Should the root `MainPage.xaml` (Blank version) remain as a separate file?** Currently the root `MainPage.xaml` (used by the Blank preset) is excluded when `useExtensionsNavigation && useDependencyInjection`. The Presentation `MainPage.xaml` will now serve a similar purpose. Consider whether these can be unified.
3. **Can SecondPage/Entity template files be deleted entirely?** Since no preset generates them after this change, they could be removed from the template rather than conditionally excluded. This simplifies the template and avoids dead code.