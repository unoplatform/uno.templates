//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.DataContext<$loginDataContext$>((page, vm) => page
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
                        .Width(200)
                        .Spacing(16)
                        .Children(
#if useCustomAuthentication
                            new TextBox()
                                .Text(x => x.Binding(() => vm.Username).TwoWay())
                                .PlaceholderText("Username")
                                .HorizontalAlignment(HorizontalAlignment.Stretch),
                            new PasswordBox()
                                .Password(x => x.Binding(() => vm.Password).TwoWay())
                                .PlaceholderText("Password")
                                .HorizontalAlignment(HorizontalAlignment.Stretch),
#endif
                            new Button()
                                .Content("Login")
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Command(() => vm.Login)))));
#else
        this.InitializeComponent();
#endif
    }
}
