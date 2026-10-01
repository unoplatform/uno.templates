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
#if (useTabBarFrame)
        ContentFrame.Navigate(typeof(Pages.HomePage));
        Tabs.SelectedIndex = 0;
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
#if (useTabBarFrame)

    private void Tabs_SelectionChanged(object sender, Uno.Toolkit.UI.TabBarSelectionChangedEventArgs args)
    {
        switch (((Uno.Toolkit.UI.TabBar)sender).SelectedIndex)
        {
            case 0:
                ContentFrame.Navigate(typeof(Pages.HomePage));
                break;
            case 1:
                ContentFrame.Navigate(typeof(Pages.AboutPage));
                break;
        }
    }
#endif
//-:cnd:noEmit
}
