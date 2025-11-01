//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.DataContext<MainViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
#if useMaterial
            .Background(Theme.Brushes.Background.Default)
#else
            .Background(ThemeResource.Get<Brush>("$themeBackgroundBrush$"))
#endif
            .Content(new Grid()
#if useToolkit
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
#endif
                .RowDefinitions("Auto,*")
                .Children(
#if useToolkit
                    new NavigationBar().Content(() => vm.Title),
#else
                    new TextBlock()
                        .Text(() => vm.Title)
                        .HorizontalAlignment(HorizontalAlignment.Center),
#endif
                    new StackPanel()
                        .Grid(row: 1)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Spacing(16)
                        .Children(
                            new TextBox()
                                .Text(x => x.Binding(() => vm.Name).Mode(BindingMode.TwoWay))
                                .PlaceholderText("Enter your name:"),
                            new Button()
                                .Content("Go to Second Page")
                                .AutomationProperties(automationId: "SecondPageButton")
#if !useAuthentication
                                .Command(() => vm.GoToSecondViewCommand)
#else
                                .Command(() => vm.GoToSecondViewCommand),
                            new Button()
                                .Content("Logout")
                                .AutomationProperties(automationId: "LogoutButton")
                                .Command(() => vm.DoLogoutCommand)
#endif
                                ))));
#else
        this.InitializeComponent();
#endif
    }
}
