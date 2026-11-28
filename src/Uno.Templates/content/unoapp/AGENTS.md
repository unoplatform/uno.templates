# Instructions for coding agents

This is an Uno Platform app. These instructions were selected for the features it was created with; a feature added later is not covered here, so check the Uno Platform docs (https://platform.uno/docs) for it. Rules that uno-lint checks show the rule name in brackets. If you use uno-lint, mark a deliberate exception with a comment directly above it: `<!-- uno-lint: allow <rule> - reason -->` in XAML, `// uno-lint: allow <rule> - reason` in C#.

## Build and run

<!--#if (useDesktop)-->
- Build: `dotnet build MyExtensionsApp.1/MyExtensionsApp.1.csproj -f $baseTargetFramework$-desktop`
- Run: `dotnet run --project MyExtensionsApp.1/MyExtensionsApp.1.csproj -f $baseTargetFramework$-desktop`
<!--#elif (useWasm)-->
- Build: `dotnet build MyExtensionsApp.1/MyExtensionsApp.1.csproj -f $baseTargetFramework$-browserwasm`
- Run: `dotnet run --project MyExtensionsApp.1/MyExtensionsApp.1.csproj -f $baseTargetFramework$-browserwasm`
<!--#else-->
- Build: `dotnet build MyExtensionsApp.1/MyExtensionsApp.1.csproj`, with `-f` set to one of the target frameworks in the .csproj.
<!--#endif-->
<!--#if (useUnitTests)-->
- Test: `dotnet test MyExtensionsApp.1.Tests/MyExtensionsApp.1.Tests.csproj`
<!--#endif-->
<!--#if (useUITests)-->
- UI tests in `MyExtensionsApp.1.UITests` drive a running app; run them after the unit tests.
<!--#endif-->
- Keep the build free of warnings. Treat `Uno0001` (an API not implemented on one of the targets) as an error: use another API, or move the call into platform-specific code.
- Look up an Uno Platform API in the docs before guessing its name or behavior.

## Project structure

<!--#if (useMvux)-->
- Pages live in `Presentation/` and bind with `{Binding}` to MVUX models (`partial record ...Model`). Async data is an `IFeed<T>`, editable state an `IState<T>`, and each public method becomes a command. Do not add MVVM view models.
<!--#elif (useMvvm)-->
- Pages live in `Presentation/` and bind to view models built with CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`). Do not add MVUX models.
<!--#endif-->
<!--#if (useDependencyInjection)-->
- Register services in `ConfigureServices` in `App.xaml.cs` and take them as constructor parameters.
<!--#endif-->
<!--#if (useExtensionsNavigation)-->
- Register routes in `RegisterRoutes` in `App.xaml.cs`. Navigate from XAML with `uen:Navigation.Request`, or from a model through an injected `INavigator`; never from code-behind.
<!--#endif-->
<!--#if (useCsharpMarkup)-->
- Pages are written in C# Markup. The XAML examples below apply to the equivalent C# Markup properties.
<!--#endif-->
- Platform-specific code goes behind an interface in shared code, with the implementation in `Platforms/<platform>` or a file such as `Scanner.Android.cs`. Models never branch on the platform (`OperatingSystem.IsAndroid()`).

## Theme and design

<!--#if (useMaterial || useSimpleTheme)-->
### Colours [HEX, CSCOLOR, TOKENTHEME]

Use the theme's role brushes (`PrimaryBrush`, `OnSurfaceBrush`, `SurfaceContainerHighBrush`, `OutlineVariantBrush`, `ErrorBrush`...). Brand colours go in the palette override (`Styles/ColorPaletteOverride.xaml`); every other colour maps to a role: status and chart colours to Primary, Secondary, Tertiary, Error and their containers. A literal colour, in XAML or in C#, does not follow light and dark mode.
<!--#if (useDspGenerator)-->
The build regenerates `ColorPaletteOverride.xaml` from `Styles/ColorPaletteOverride.json`: edit the .json.
<!--#endif-->

```xaml
<!-- Don't --> <TextBlock Text="Overdue" Foreground="#D32F2F" />
<!-- Do -->    <TextBlock Text="Overdue" Foreground="{ThemeResource ErrorBrush}" />
```
<!--#else-->
### Colours [HEX, CSCOLOR, TOKENTHEME]

Use WinUI theme brushes (`TextFillColorPrimaryBrush`, `CardBackgroundFillColorDefaultBrush`, `AccentFillColorDefaultBrush`, `SystemFillColorCriticalBrush`...). An app colour is defined once per theme in a `ResourceDictionary.ThemeDictionaries` (Light and Dark) in App.xaml. A literal colour, in XAML or in C#, does not follow light and dark mode.

```xaml
<!-- Don't --> <TextBlock Text="Overdue" Foreground="#D32F2F" />
<!-- Do -->    <TextBlock Text="Overdue" Foreground="{ThemeResource SystemFillColorCriticalBrush}" />
```
<!--#endif-->

In C#, take the brush from a dependency property bound with `{ThemeResource}`, and redraw on `ActualThemeChanged` if you paint it yourself.

### Typography

Text uses the theme's text styles; do not set `FontSize` or `FontWeight` on a TextBlock.
<!--#if (useMaterial || useSimpleTheme)-->
The styles follow the Material type scale: `DisplayLarge` to `DisplaySmall`, `Headline*`, `Title*`, `Body*`, `Label*` (each Large, Medium, Small).

```xaml
<!-- Don't --> <TextBlock Text="Orders" FontSize="22" FontWeight="SemiBold" />
<!-- Do -->    <TextBlock Text="Orders" Style="{StaticResource TitleLarge}" />
```
<!--#else-->
Use the WinUI type ramp: `CaptionTextBlockStyle`, `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, `SubtitleTextBlockStyle`, `TitleTextBlockStyle`, `TitleLargeTextBlockStyle`, `DisplayTextBlockStyle`.

```xaml
<!-- Don't --> <TextBlock Text="Orders" FontSize="20" FontWeight="SemiBold" />
<!-- Do -->    <TextBlock Text="Orders" Style="{StaticResource SubtitleTextBlockStyle}" />
```
<!--#endif-->
<!--#if (useMaterial || useSimpleTheme)-->

### Spacing [SPACING]

Margin, Padding, Spacing, RowSpacing and ColumnSpacing use the theme's spacing tokens, so the whole app can be rescaled from the theme (`DefaultSpacing`, `DefaultDensity`). The tokens are generated at runtime, so you will not find them in the project's files.

- Scale (8 px base): `Space025`=2, `Space050`=4, `Space075`=6, `Space100`=8, `Space150`=12, `Space200`=16, `Space250`=20, `Space300`=24, `Space400`=32, `Space500`=40, `Space600`=48.
- `Spacing`, `RowSpacing`, `ColumnSpacing` take the number: `{StaticResource Space200}`.
- `Margin` and `Padding` take a Thickness: `Space200Thickness` (all sides), `Space200HorizontalThickness`, `Space200VerticalThickness`, `Space200TopThickness` (also Bottom, Left, Right).
- Snap a value that is off the scale to the nearest step. `0` needs no token.

```xaml
<!-- Don't --> <StackPanel Padding="16" Spacing="8" />
<!-- Do -->    <StackPanel Padding="{StaticResource Space200Thickness}" Spacing="{StaticResource Space100}" />
```

### Corner radius [RADIUS]

`CornerRadius` uses `Radius100CornerRadius` (4), `Radius200CornerRadius` (8) or `Radius400CornerRadius` (16); the theme rescales them with `DefaultCornerRadius`.

```xaml
<!-- Don't --> <Button Content="Save" CornerRadius="8" />
<!-- Do -->    <Button Content="Save" CornerRadius="{StaticResource Radius200CornerRadius}" />
```
<!--#endif-->

### Icons [ICON]

Use `SymbolIcon` or `FontIcon` (the Fluent symbol font ships with Uno Platform), or path data from one open icon set (Material Symbols, Fluent System Icons) kept as keyed resources in one `Icons.xaml` and used through `PathIcon Data="{StaticResource ...}"`. Never draw an icon from shapes or paste path data into a page. An illustration is not an icon: give it an allow comment.

```xaml
<!-- Don't --> <PathIcon Data="M12,2 L22,22 L2,22 Z" />
<!-- Do -->    <SymbolIcon Symbol="Favorite" />
```

## Controls

### Use the control that ships [BUILTIN]

Before writing a custom control, use the control that ships with the platform or a toolkit, adjusted with lightweight styling, then Windows Community Toolkit 8.x. A custom control misses the theme, accessibility and fixes the shipped one gets.

- Rating: `RatingControl`. Progress: `ProgressBar`, `ProgressRing`. Pager dots: `PipsPager`. On/off: `ToggleSwitch`.
<!--#if (useToolkit)-->
- Back bar: `utu:NavigationBar`. Chips: `utu:Chip`, `utu:ChipGroup`. Tabs: `utu:TabBar`.
<!--#if (useMaterial || useSimpleTheme)-->
- Card: `utu:CardContentControl`.
<!--#endif-->
<!--#endif-->
<!--#if (useToolkit)-->
<!--#if (useMaterial || useSimpleTheme)-->

### Cards [CARD]

A rounded, filled or outlined container is a `utu:CardContentControl` (`FilledCardContentControlStyle`, `OutlinedCardContentControlStyle`, `ElevatedCardContentControlStyle`), tuned with its lightweight keys (`CardCornerRadius`, `CardPadding`...). A bare `Border` is for dividers and image clipping.

```xaml
<!-- Don't --> <Border CornerRadius="8" Background="{ThemeResource SurfaceBrush}"><TextBlock Text="Order 1042" /></Border>
<!-- Do -->    <utu:CardContentControl Style="{StaticResource FilledCardContentControlStyle}"><TextBlock Text="Order 1042" /></utu:CardContentControl>
```
<!--#endif-->
<!--#endif-->
<!--#if (useToolkit)-->
<!--#if (useExtensionsNavigation)-->

### Back navigation [BACKBAR]

A page with a back action uses `utu:NavigationBar`; back navigation and the Android back gesture come with it.

```xaml
<!-- Don't --> <Button uen:Navigation.Request="-" Content="Back" />
<!-- Do -->    <utu:NavigationBar Content="Details" />
```
<!--#endif-->
<!--#endif-->

### Sheets, drawers and overlays [OVERLAY]

<!--#if (useExtensionsNavigation)-->
Open a sheet, filter panel or drawer by navigating to a Page with `uen:Navigation.Request="!Filters"` (it opens as a flyout) and style it with a `DrawerFlyoutPresenter` style. Its state lives in the model. A panel shown by toggling `Visibility` from code-behind misses focus, dismissal and back navigation.
<!--#else-->
Use a `Flyout` or `ContentDialog`, or bind the panel's `Visibility` to model state. A panel shown by toggling `Visibility` from code-behind misses focus, dismissal and back navigation.
<!--#endif-->

## Architecture
<!--#if (useMvvm || useMvux)-->

### Code-behind [CODEBEHIND]

Page code-behind holds `InitializeComponent` plus UI-only glue that has no XAML form (focus, animation, a platform API). Actions are commands on the model, and visual changes follow model state through bindings or Visual State Manager states.
<!--#if (useToolkit)-->
For events without a `Command` property use `utu:CommandExtensions.Command`; to drive states from the model use `utu:VisualStateManagerExtensions.States`.
<!--#endif-->

```xaml
<!-- Don't --> <Button Content="Save" Click="OnSaveClick" />
<!--#if (useMvux)-->
<!-- Do -->    <Button Content="Save" Command="{Binding Save}" />
<!--#else-->
<!-- Do -->    <Button Content="Save" Command="{Binding SaveCommand}" />
<!--#endif-->
```
<!--#endif-->

### Responsive layout [RESPONSIVE]

<!--#if (useToolkit)-->
Width-dependent values are set in XAML with `{utu:Responsive}` (custom thresholds in a `utu:ResponsiveLayout` resource), or `utu:ResponsiveView` when the whole layout changes. Never compare widths in code-behind or keep width flags on a model.

```xaml
<!-- Don't --> <!-- SizeChanged handler: Panel.Orientation = e.NewSize.Width > 800 ? Horizontal : Vertical -->
<!-- Do -->    <StackPanel Orientation="{utu:Responsive Narrow=Vertical, Wide=Horizontal}" />
```
<!--#else-->
Width-dependent layout is a Visual State Manager state with an `AdaptiveTrigger` in XAML. Never compare widths in code-behind or keep width flags on a model.

```xaml
<!-- Don't --> <!-- SizeChanged handler: Panel.Orientation = e.NewSize.Width > 800 ? Horizontal : Vertical -->
<!-- Do -->    <VisualState><VisualState.StateTriggers><AdaptiveTrigger MinWindowWidth="800" /></VisualState.StateTriggers>
                 <VisualState.Setters><Setter Target="Panel.Orientation" Value="Horizontal" /></VisualState.Setters></VisualState>
```
<!--#endif-->

## Binding and async
<!--#if (useMvux)-->

### MVUX state bindings [BINDVALUE]

Bind the state itself; the generated view model already exposes its value. A two-way binding to `.Value` shows nothing and writes nothing back. Inside a `FeedView` template, read data with `{Binding Data.X}`.

```xaml
<!-- Don't --> <TextBox Text="{Binding City.Value, Mode=TwoWay}" />
<!-- Do -->    <TextBox Text="{Binding City, Mode=TwoWay}" />
```
<!--#endif-->

### x:Bind mode

`x:Bind` is `OneTime` by default, so a value that changes later never shows. Add `Mode=OneWay` for anything that changes.

### Never block on a task

`.Result`, `.Wait()` and `GetAwaiter().GetResult()` freeze the UI thread and can deadlock. Await the task; if the caller cannot be async (a constructor, a property), start the load from a command or, in MVUX, a feed.
