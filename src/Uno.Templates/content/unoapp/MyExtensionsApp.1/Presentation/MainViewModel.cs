namespace MyExtensionsApp._1.Presentation;

public partial class MainViewModel : ObservableObject
{
//+:cnd:noEmit
#if useAuthentication
	private IAuthenticationService _authentication;

#endif
//-:cnd:noEmit
	private INavigator _navigator;

	[ObservableProperty]
	private string? name;

//+:cnd:noEmit
	public MainViewModel(
#if useLocalization
		IStringLocalizer localizer,
#endif
#if useConfiguration
		IOptions<AppConfig> appInfo,
#endif
#if useAuthentication
		IAuthenticationService authentication,
#endif
		INavigator navigator)
	{
		_navigator = navigator;
#if useAuthentication
		_authentication = authentication;
#endif
		Title = "Main";
#if useLocalization
		Title += $" - {localizer["ApplicationName"]}";
#endif
#if useConfiguration
		Title += $" - {appInfo?.Value?.Environment}";
#endif
		GoToSecond = new AsyncRelayCommand(GoToSecondView);
#if useAuthentication
		Logout = new AsyncRelayCommand(DoLogout);
#endif
//-:cnd:noEmit
	}
	public string? Title { get; }

	public ICommand GoToSecond { get; }

//+:cnd:noEmit
#if useAuthentication
	public ICommand Logout { get; }

#endif
//-:cnd:noEmit

	private async Task GoToSecondView()
	{
		await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new Entity(Name!));
	}

//+:cnd:noEmit
#if useAuthentication
	public async Task DoLogout(CancellationToken token)
	{
		await _authentication.LogoutAsync(token);
	}
#endif
//-:cnd:noEmit
}
