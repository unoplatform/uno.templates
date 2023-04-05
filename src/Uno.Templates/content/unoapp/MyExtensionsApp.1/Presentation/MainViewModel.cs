using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyExtensionsApp._1.Presentation;

public partial class MainViewModel : ObservableObject
{
	public string? Title { get; }

	[ObservableProperty]
	private string? name;

	public ICommand GoToSecond { get; }

//+:cnd:noEmit
#if useAuthentication
	public ICommand Logout { get; }

#endif
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

	private IAuthenticationService _authentication;
#endif
//-:cnd:noEmit

	private INavigator _navigator;
}
