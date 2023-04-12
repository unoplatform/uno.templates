//-:cnd:noEmit
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyExtensionsApp._1.Presentation;

public partial class LoginViewModel : ObservableObject
{
	private IAuthenticationService _authentication;

	private INavigator _navigator;

	[ObservableProperty]
	private string? _username;

	[ObservableProperty]
	private string? _password;

	public LoginViewModel(
		INavigator navigator,
		IAuthenticationService authentication)
	{
		_navigator = navigator;
		_authentication = authentication;
		Login = new AsyncRelayCommand(DoLogin);
	}

	private async Task DoLogin()
	{
        var success = await _authentication.LoginAsync(new Dictionary<string, string> { { nameof(Username), Username ?? string.Empty }, { nameof(Password), Password ?? string.Empty } });
        if (success)
        {
            await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack);
        }
	}

	public string Title { get; } = "Login";

	public ICommand Login { get; }
}
