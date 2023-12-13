//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public partial record LoginModel(IDispatcher Dispatcher, INavigator Navigator, IAuthenticationService Authentication)
{
    public string Title { get; } = "Login";

 //+:cnd:noEmit
#if useCustomAuthentication
    public IState<string> Username => State<string>.Value(this, () => string.Empty);

    public IState<string> Password => State<string>.Value(this, () => string.Empty);
#endif

    public async ValueTask Login(CancellationToken token)
    {
#if useCustomAuthentication
        var username = await Username ?? string.Empty;
        var password = await Password ?? string.Empty;

        var success = await Authentication.LoginAsync(Dispatcher, new Dictionary<string, string> { { nameof(Username), username }, { nameof(Password), password } });
#else
        var success = await Authentication.LoginAsync(Dispatcher);
#endif
//-:cnd:noEmit
        if (success)
        {
            await Navigator.NavigateViewModelAsync<MainModel>(this, qualifier: Qualifiers.ClearBackStack);
        }
    }

}
