//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public partial record MainModel
{
	private INavigator _navigator;

//+:cnd:noEmit
	public MainModel(
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
//-:cnd:noEmit
	}

	public string? Title { get; }

	public IState<string> Name => State<string>.Value(this, () => string.Empty);

	public async Task GoToSecond()
	{
		var name = await Name;
		await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(name!));
	}

//+:cnd:noEmit
#if useAuthentication
	public async ValueTask Logout(CancellationToken token)
	{
		await _authentication.LogoutAsync(token);
	}

	private IAuthenticationService _authentication;
#endif
//-:cnd:noEmit
}
