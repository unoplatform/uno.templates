//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public class ShellViewModel
{
//+:cnd:noEmit
#if useAuthentication
    private readonly IAuthenticationService _authentication;


#endif
//-:cnd:noEmit
    private readonly INavigator _navigator;

    public ShellViewModel(
//+:cnd:noEmit
#if useAuthentication
        IAuthenticationService authentication,
#endif
        INavigator navigator)
    {
        _navigator = navigator;
#if useAuthentication
        _authentication = authentication;
        _authentication.LoggedOut += LoggedOut;
    }

    private async void LoggedOut(object? sender, EventArgs e)
    {
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
#else
        _ = Start();
    }

    public async Task Start()
    {
        await _navigator.NavigateViewModelAsync<MainViewModel>(this);
    }
#endif
//-:cnd:noEmit
}
