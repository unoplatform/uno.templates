//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public partial class MainViewModel : ObservableObject
{
//+:cnd:noEmit
#if useAuthentication
    private IAuthenticationService _authentication;

#endif
    private INavigator _navigator;

    [ObservableProperty]
    private string? name;
#if mauiEmbedding

    private int count = 0;

    [ObservableProperty]
    private string counterText = "Press Me";
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
#if mauiEmbedding
        Counter = new RelayCommand(OnCount);
#endif
#if useAuthentication
        Logout = new AsyncRelayCommand(DoLogout);
#endif
    }
    public string? Title { get; }

    public ICommand GoToSecond { get; }
#if mauiEmbedding

    public ICommand Counter { get; }
#endif

#if useAuthentication
    public ICommand Logout { get; }

#endif
    private async Task GoToSecondView()
    {
        await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new Entity(Name!));
    }

#if mauiEmbedding

    private void OnCount()
    {
        CounterText = ++count switch
        {
            1 => "Pressed Once!",
            _ => $"Pressed {count} times!"
        };
    }
#endif
#if useAuthentication
    public async Task DoLogout(CancellationToken token)
    {
        await _authentication.LogoutAsync(token);
    }
#endif
//-:cnd:noEmit
}
