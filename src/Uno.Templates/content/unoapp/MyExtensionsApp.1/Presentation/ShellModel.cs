//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public class ShellModel
{
    private readonly INavigator _navigator;

    public ShellModel(
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
        await _navigator.NavigateViewModelAsync<LoginModel>(this, qualifier: Qualifiers.ClearBackStack);
    }

    private readonly IAuthenticationService _authentication;
#else
        // Add code here to initialize or attach event handlers to singleton services
    }
#endif
//-:cnd:noEmit
}
