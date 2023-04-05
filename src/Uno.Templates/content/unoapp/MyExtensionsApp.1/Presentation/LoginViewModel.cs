using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyExtensionsApp._1.Presentation;

public partial class LoginViewModel : ObservableObject
{
	public string Title { get; } = "Login";

	[ObservableProperty]
	private string? _username;

	[ObservableProperty]
	private string? _password;

	public ICommand Login { get; }

//+:cnd:noEmit
#if useLocalization
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
        var success = await Authentication.LoginAsync(new Dictionary<string, string> { { nameof(Username), Username }, { nameof(Password), Password } });
        if (success)
        {
            await Navigator.NavigateViewModelAsync<MainModel>(this, qualifier: Qualifiers.ClearBackStack);
        }
	}

	private INavigator _navigator;
	private IAuthenticationService _authentication;
}
