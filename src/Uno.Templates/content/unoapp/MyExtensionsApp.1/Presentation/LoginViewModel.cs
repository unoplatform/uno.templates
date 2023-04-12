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
        var success = await _authentication.LoginAsync(new Dictionary<string, string> { { nameof(Username), Username ?? string.Empty }, { nameof(Password), Password ?? string.Empty } });
        if (success)
        {
            await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack);
        }
	}

	private INavigator _navigator;
	private IAuthenticationService _authentication;
}
