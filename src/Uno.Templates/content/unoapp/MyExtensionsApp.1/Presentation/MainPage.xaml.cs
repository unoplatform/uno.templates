//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.DataContext<$mainDataContext$>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
#if useMaterial
            .Background(Theme.Brushes.Background.Default)
#else
            .Background(ThemeResource.Get<Brush>("$themeBackgroundBrush$"))
#endif
            .Content(new Grid()
#if useToolkit
                .SafeArea(SafeArea.InsetMask.All)
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
                                .Command(() => vm.GoToSecond)
#else
                                .Command(() => vm.GoToSecond),
                            new Button()
                                .Content("Logout")
                                .AutomationProperties(automationId: "LogoutButton")
                                .Command(() => vm.Logout)
#endif
                                ))));
#else
        this.InitializeComponent();
#endif
    }
}
