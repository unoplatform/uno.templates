namespace MyExtensionsApp._1.Presentation;

public sealed partial class LoginPage : Page
{
	public LoginPage()
	{
//+:cnd:noEmit
#if useCsharpMarkup
		this.DataContext<$mainDataContext$>((page, vm) => page
			.NavigationCacheMode(NavigationCacheMode.Required)
			.Background(Theme.Brushes.Background.Default)
			.Content(new Grid()
#if useToolkit					
				.SafeArea(SafeArea.InsetMask.All)
#endif
				.RowDefinitions<Grid>("Auto,*")
				.Children(
#if useToolkit					
					new NavigationBar().Content(() => vm.Title),
#else
				new TextBlock()
					.Text(() => vm.Title)
					.HorizontalAlignment(HorizontalAlignment.Center)
#endif
					new StackPanel()
						.Grid(row: 1)
						.HorizontalAlignment(HorizontalAlignment.Center)
						.VerticalAlignment(VerticalAlignment.Center)
						.Width(200)
						.Spacing(16)
						.Children(
							new TextBox()
								.Text(x => x.Bind(() => vm.Name).Mode(BindingMode.TwoWay))
								.PlaceholderText("Username")
								.HorizontalAlignment(HorizontalAlignment.Stretch),
                            new PasswordBox()
                                .Password(x => x.Bind(() => vm.Password).Mode(BindingMode.TwoWay))
                                .PlaceholderText("Password")
                                .HorizontalAlignment(HorizontalAlignment.Stretch),
                            new Button()
								.Content("Login")
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Command(() => vm.Login)))));
#else
		this.InitializeComponent();
#endif
	}
}
