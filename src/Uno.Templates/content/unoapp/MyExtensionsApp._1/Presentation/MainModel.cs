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
	}

	public string? Title { get; }

	public IState<string> Name => State<string>.Value(this, () => string.Empty);
#if mauiEmbedding

	private int _currentCount = 0;

	public IState<string> CounterText => State<string>.Value(this, () => "Press Me");

	public async Task Counter()
	{
		var message = ++_currentCount switch
		{
			1 => "Pressed Once!",
			_ => $"Pressed {_currentCount} times!"
		};
		await CounterText.Set(message, default);
	}
#endif

	public async Task GoToSecond()
	{
		var name = await Name;
		await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(name!));
	}

#if useAuthentication
	public async ValueTask Logout(CancellationToken token)
	{
		await _authentication.LogoutAsync(token);
	}

	private IAuthenticationService _authentication;
#endif
//-:cnd:noEmit
}
