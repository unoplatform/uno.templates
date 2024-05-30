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
        // Add code here to initialize or attach event handlers to singleton services
        // for example the LoggedOut event of an authentication service.
    }
#endif
//-:cnd:noEmit
}
