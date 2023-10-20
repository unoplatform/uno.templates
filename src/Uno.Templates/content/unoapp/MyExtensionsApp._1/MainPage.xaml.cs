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
#endif
#if themeService
        Loaded += MainPage_Loaded;
#endif
    }

#if themeService
    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        this.ThemeSwitcher.IsChecked = this.GetThemeService().IsDark;
    }

    private void ThemeSwitcher_CheckChanged(object sender, RoutedEventArgs e)
    {
        var isDark = ThemeSwitcher.IsChecked ?? false;
        this.GetThemeService().SetThemeAsync(isDark ? AppTheme.Dark : AppTheme.Light);
    }
#endif

//-:cnd:noEmit
}
