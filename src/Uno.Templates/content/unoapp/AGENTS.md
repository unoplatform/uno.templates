# Rules for coding agents

Follow these rules when you write or change XAML and C# in this Uno Platform project. They were selected for the features the project was created with. A rule marked "needs" applies only while the project has that feature (check `UnoFeatures` in the .csproj). A deliberate exception gets `uno-lint: allow <RULE> - reason` in a comment directly above it.

## What the project has

<!--#if (useToolkit)-->
- **Uno Toolkit**: UnoFeatures Toolkit, Uno.Toolkit* package, using:Uno.Toolkit.UI
<!--#endif-->
<!--#if (useMaterial || useSimpleTheme)-->
- **An Uno.Themes theme: Material or Simple, not Fluent**: App.xaml merges MaterialTheme, MaterialToolkitTheme, SimpleTheme or SimpleToolkitTheme; UnoFeatures Material or SimpleTheme. All of them inherit Uno.Themes BaseTheme and generate the Space* and Radius* tokens at runtime, so the tokens never appear in the project's files. Fluent is the default WinUI look: an app with none of these themes has no Space* or Radius* tokens, and the rules that need this theme do not apply.
<!--#endif-->
<!--#if (useMaterial)-->
- **Material theme**: UnoFeatures Material, Uno.Material
<!--#endif-->
<!--#if (useExtensionsNavigation)-->
- **Uno.Extensions Navigation**: UnoFeatures Navigation
<!--#endif-->
<!--#if (useMvux)-->
- **MVUX**: UnoFeatures MVUX, Uno.Extensions.Reactive
<!--#endif-->

## Code rules

<!--#if (useMvux)-->
### BINDVALUE (needs MVUX)

The MVUX bindable proxy already exposes the state's value; a two-way binding to .Value on an input does not write back.

Don't:

```xaml
<TextBox Text="{Binding City.Value, Mode=TwoWay}" />
```

Do:

```xaml
<TextBox Text="{Binding City, Mode=TwoWay}" />
```

Fix: bind the state itself on a two-way input: {Binding State, Mode=TwoWay}. The MVUX proxy already exposes the value. Inside a FeedView template bind {Binding Data.X}.

<!--#endif-->
### BUILTIN

A re-implemented control misses the theme, accessibility and fixes the shipped one gets.

Don't:

```cs
public sealed class StarRating : Control { }
```

Do:

```cs
public sealed class OrderSummary : Control { }
```

Fix: use the platform/Toolkit control named above with lightweight styling before writing a custom control; then Windows Community Toolkit 8.x.

### CSCOLOR

Colours chosen in code skip the theme entirely and never change with light/dark.

Don't:

```cs
public class Legend
{
    Color Overdue => Colors.Red;
}
```

Do:

```cs
public class Legend
{
    Brush Overdue => (Brush)Application.Current.Resources["ErrorBrush"];
}
```

Fix: read the colour from theme resources (a Brush dependency property bound with {ThemeResource ...Brush}, or a lookup keyed on ActualTheme) and redraw on ActualThemeChanged.

### HEX

A literal colour does not follow light/dark or a brand change; the theme's palette is the one place colours belong.

Don't:

```xaml
<TextBlock Text="Overdue" Foreground="#D32F2F" />
```

Do:

```xaml
<TextBlock Text="Overdue" Foreground="{ThemeResource ErrorBrush}" />
```

Fix: a theme brush ({ThemeResource ...Brush}) from the app's theme, or a colour defined once in the theme's colour override file. A literal colour does not follow light/dark or brand changes.

<!--#if (useMaterial)-->
With material: a Material role brush (PrimaryBrush, OnSurfaceBrush, SurfaceContainerHighBrush, OutlineVariantBrush, ErrorBrush...). The only colours an app defines live in its ColorPaletteOverride file; brand colour goes there. Status and chart colours map to Primary/Secondary/Tertiary/Error and their containers.

<!--#endif-->
### ICON

Hand-drawn icons drift in size and stroke and cannot be swapped as a set.

Don't:

```xaml
<PathIcon Data="M12,2 L22,22 L2,22 Z" />
```

Do:

```xaml
<SymbolIcon Symbol="Favorite" />
```

Fix: SymbolIcon/FontIcon on the Fluent symbol font, or keyed path data from one open icon set (Material Symbols, Fluent System Icons) in Icons.xaml via PathIcon Data="{StaticResource ...}". Never hand-draw icons. Illustrations get "xaml-lint: allow icon - illustration".

### PLATFORMLEAK

Platform branches in models make them untestable on other targets and scatter platform logic.

Don't:

```cs
public partial class CameraViewModel
{
    bool CanScan => OperatingSystem.IsAndroid();
}
```

Do:

```cs
public partial class CameraViewModel(IScannerService scanner)
{
    bool CanScan => scanner.IsAvailable;
}
```

Fix: move the branch into a platform service (interface in shared code, implementation per head or in a Platforms/ partial) and inject it; models stay platform-neutral.

### RESPONSIVE

Two breakpoint systems in one app drift apart; width logic in code-behind cannot be previewed or tested.

Don't:

```xaml
<VisualStateManager.VisualStateGroups><VisualStateGroup><VisualState>
  <VisualState.StateTriggers><AdaptiveTrigger MinWindowWidth="720" /></VisualState.StateTriggers>
</VisualState></VisualStateGroup></VisualStateManager.VisualStateGroups>
```

Do:

```xaml
<StackPanel Orientation="{utu:Responsive Narrow=Vertical, Wide=Horizontal}" />
```

Fix: move the breakpoint into XAML: a VisualStateManager state with an AdaptiveTrigger (or add Uno Toolkit and use {utu:Responsive}). Never compare widths in code-behind or keep width flags on a view model.

<!--#if (useToolkit)-->
With toolkit: breakpoints are always {utu:Responsive} in XAML for a value, utu:ResponsiveView when the whole template changes (custom thresholds via a utu:ResponsiveLayout resource). No WinUI AdaptiveTrigger (not suppressible), never code-behind, never width flags on a view model.

<!--#endif-->
### SYNCWAIT

Blocking on a task freezes the UI thread and can deadlock.

Don't:

```cs
public class OrderLoader
{
    string Load(HttpClient http) => http.GetStringAsync("/orders").Result;
}
```

Do:

```cs
public class OrderLoader
{
    async Task<string> Load(HttpClient http) => await http.GetStringAsync("/orders");
}
```

Fix: await the task. If the caller cannot be async (constructor, property), start the load from a command or MVUX feed instead of blocking.

### TOKENTHEME

A brush defined once outside ThemeDictionaries keeps its light-mode value in dark mode.

Don't:

```xaml
<Page.Resources>
  <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource BrandColor}" />
</Page.Resources>
```

Do:

```xaml
<Page.Resources>
  <ResourceDictionary>
    <ResourceDictionary.ThemeDictionaries>
      <ResourceDictionary x:Key="Light"><SolidColorBrush x:Key="AccentBrush" Color="{StaticResource BrandColor}" /></ResourceDictionary>
      <ResourceDictionary x:Key="Dark"><SolidColorBrush x:Key="AccentBrush" Color="{StaticResource BrandColorDark}" /></ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
  </ResourceDictionary>
</Page.Resources>
```

Fix: use a theme role brush; else define the key per theme in ThemeDictionaries; name it *Invariant/*OnDark/*OnLight/*Fixed only when its ground is fixed in both themes.

### XBINDMODE

x:Bind defaults to OneTime, so a value that changes later never shows.

Don't:

```xaml
<TextBlock Text="{x:Bind ViewModel.Title}" />
```

Do:

```xaml
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />
```

Fix: x:Bind is OneTime by default: add Mode=OneWay for values that change, or a comment saying OneTime is intended.

## Design-system rules

<!--#if (useToolkit)-->
### BACKBAR (needs Uno Toolkit)

A hand-built back button misses the platform back gesture and the shared app bar layout.

Don't:

```xaml
<Button uen:Navigation.Request="-" Content="Back" />
```

Do:

```xaml
<utu:NavigationBar Content="Details">
  <utu:NavigationBar.MainCommand><AppBarButton uen:Navigation.Request="-" /></utu:NavigationBar.MainCommand>
</utu:NavigationBar>
```

Fix: utu:NavigationBar with a MainCommand AppBarButton; back navigation and the Android back gesture come with it.

<!--#endif-->
<!--#if (useToolkit)-->
### CARD (needs Uno Toolkit)

A hand-drawn card ignores the theme's card styles and states, so every card in the app drifts a little differently.

Don't:

```xaml
<Border CornerRadius="{StaticResource Radius200CornerRadius}" Background="{ThemeResource SurfaceBrush}">
  <StackPanel><TextBlock Text="Order 1042" /><TextBlock Text="3 items" /></StackPanel>
</Border>
```

Do:

```xaml
<utu:CardContentControl Style="{StaticResource FilledCardContentControlStyle}">
  <StackPanel><TextBlock Text="Order 1042" /><TextBlock Text="3 items" /></StackPanel>
</utu:CardContentControl>
```

Fix: utu:CardContentControl (Filled/Outlined/ElevatedCardContentControlStyle) + lightweight keys (CardCornerRadius, CardPadding, <Variant>CardContentBackground/BorderBrush).

<!--#endif-->
### CODEBEHIND

Logic in code-behind cannot be tested and spreads: one workaround gets copied to every page.

Don't:

```cs
public sealed partial class OrdersPage : Page
{
    private void OnSaveClick(object sender, RoutedEventArgs e) { }
}
```

Do:

```cs
public sealed partial class OrdersPage : Page
{
    public OrdersPage() => this.InitializeComponent();
}
```

Fix: commands on the model (ICommand), VisualStateManager states driven by model state, or Visibility bound to the model.

<!--#if (useToolkit)-->
With toolkit: utu:CommandExtensions.Command, commands on the model, utu:VisualStateManagerExtensions.States bound to model state, or Visibility bound to the model.

<!--#endif-->
### OVERLAY

Hand-toggled overlays miss focus handling, dismissal and back navigation.

Don't:

```cs
public sealed partial class SearchPage : Page
{
    void ShowFilters() => FilterSheet.Visibility = Visibility.Visible;
}
```

Do:

```cs
public sealed partial class SearchPage : Page
{
    public SearchPage() => this.InitializeComponent();
}
```

Fix: a Flyout or ContentDialog, or Visibility bound to model state; filter state lives in the model.

<!--#if (useExtensionsNavigation)-->
With navigation: uen:Navigation.Request="!Route" to a Page (opens as a flyout) with a DrawerFlyoutPresenter style; filter state lives in the model.

<!--#endif-->
<!--#if (useMaterial || useSimpleTheme)-->
### RADIUS (needs An Uno.Themes theme: Material or Simple, not Fluent)

Literal radii cannot be rescaled from the theme (DefaultCornerRadius) and drift between controls. This applies to every CornerRadius you write, including inside DataTemplates and Styles.

Don't:

```xaml
<Button Content="Save" CornerRadius="8" />
```

Do:

```xaml
<Button Content="Save" CornerRadius="{StaticResource Radius200CornerRadius}" />
```

Fix: Use the Uno.Themes Radius* tokens: CornerRadius="{StaticResource Radius200CornerRadius}". They exist in every app with an Uno.Themes theme (Material or Simple, including the Toolkit variants such as SimpleToolkitTheme); they are generated at runtime, so you will not find them in the project's files. Scale (4 px base, Radius{n} = 4 x n/100, unchanged in Uno.Themes 9): Radius100=4, Radius200=8, Radius400=16. Snap off-scale values to the nearest step (6 -> Radius100 or Radius200, 12 -> Radius200 or Radius400). Rescale the whole app with DefaultCornerRadius on the theme.

<!--#endif-->
<!--#if (useMaterial || useSimpleTheme)-->
### SPACING (needs An Uno.Themes theme: Material or Simple, not Fluent)

Literal spacing cannot be rescaled from the theme (DefaultSpacing, DefaultDensity) and drifts between screens. This applies to every Margin, Padding, Spacing, RowSpacing and ColumnSpacing you write, including inside DataTemplates and Styles; 0 is the only literal allowed.

Don't:

```xaml
<StackPanel Margin="0,8,0,0" Padding="16" Spacing="4" />
```

Do:

```xaml
<StackPanel Margin="{StaticResource Space100TopThickness}" Padding="{StaticResource Space200Thickness}" Spacing="{StaticResource Space050}" />
```

Fix: Use the Uno.Themes Space* tokens. They exist in every app with an Uno.Themes theme (Material or Simple, including the Toolkit variants such as SimpleToolkitTheme); they are generated at runtime, so you will not find them in the project's files. Scale (Uno.Themes 9+, 8 px base, Space{n} = 8 x n/100): Space025=2, Space050=4, Space075=6, Space100=8, Space125=10, Space150=12, Space200=16, Space250=20, Space300=24, Space400=32, Space500=40, Space600=48. Spacing/RowSpacing/ColumnSpacing take the double: {StaticResource Space200}. Margin/Padding take a Thickness companion: Space{n}Thickness (all sides), Space{n}HorizontalThickness (n,0,n,0), Space{n}VerticalThickness (0,n,0,n), Space{n}TopThickness, BottomThickness, LeftThickness, RightThickness. Snap off-scale values to the nearest step (3 -> Space050, 14 -> Space150 or Space200). For a two-axis value like 16,12 use the dominant axis token or split it: Padding on the container, Spacing on the inner panel. Before Uno.Themes 9 the base was 4: halve the number (16 px was Space400).

<!--#endif-->
