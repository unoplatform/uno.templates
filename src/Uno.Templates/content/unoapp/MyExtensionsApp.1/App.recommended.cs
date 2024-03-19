using Uno.Resizetizer;

//-:cnd:noEmit
namespace MyExtensionsApp._1;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

//+:cnd:noEmit
#if useFrameNav
    protected override void OnLaunched(LaunchActivatedEventArgs args)
#else
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
#endif
    {
#if useCsharpMarkup
        // Load WinUI Resources
        Resources.Build(r => r.Merged(
            new XamlControlsResources()));
#if useMaterial

#if useToolkit
        // Load Uno.UI.Toolkit and Material Resources
        Resources.Build(r => r.Merged(
            new  MaterialToolkitTheme(
                    new Styles.ColorPaletteOverride(),
                    new Styles.MaterialFontsOverride())));
#else
        // Load Uno.UI.Toolkit and Material Resources
        Resources.Build(r => r.Merged(
            new  MaterialTheme(
                    new Styles.ColorPaletteOverride(),
                    new Styles.MaterialFontsOverride())));
#endif
#elif (useToolkit)

        // Load Uno.UI.Toolkit Resources
        Resources.Build(r => r.Merged(
            new ToolkitResources()));
#endif
#endif
        var builder = this.CreateBuilder(args)
#if (useNavigationToolkit)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
#endif
//+:cnd:noEmit
#if mauiEmbedding
//-:cnd:noEmit
#if MAUI_EMBEDDING
            .UseMauiEmbedding<MauiControls.App>(maui => maui
                .UseMauiControls())
#endif
//+:cnd:noEmit
#endif
//-:cnd:noEmit
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
//+:cnd:noEmit
#if useLogging
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
#endif
#if useSerilog
                .UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
#endif
#if useConfiguration
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
#endif
#if useLocalization
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
#endif
#if useHttp
                // Register Json serializers (ISerializer and ISerializer)
                .UseSerialization((context, services) => services
                    .AddContentSerializer(context)
                    .AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
                .UseHttp((context, services) => services
                    // Register HttpClient
//-:cnd:noEmit
#if DEBUG
                    // DelegatingHandler will be automatically injected into Refit Client
                    .AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
//+:cnd:noEmit
                    .AddSingleton<IWeatherCache, WeatherCache>()
                    .AddRefitClient<IApiClient>(context))
#endif
#if useAuthentication
                .UseAuthentication(auth =>
#if useWebAuthentication
    auth.AddWeb(name: "WebAuthentication")
#elif useOidcAuthentication
    auth.AddOidc(name: "OidcAuthentication")
#elif useMsalAuthentication
    auth.AddMsal(name: "MsalAuthentication")
#elif useCustomAuthentication
    auth.AddCustom(custom =>
            custom
                .Login((sp, dispatcher, credentials, cancellationToken) =>
                {
                    // TODO: Write code to process credentials that are passed into the LoginAsync method
                    if (credentials?.TryGetValue(nameof($loginRouteViewModel$.Username), out var username) ?? false &&
                        !username.IsNullOrEmpty())
                    {
                        // Return IDictionary containing any tokens used by service calls or in the app
                        credentials ??= new Dictionary<string, string>();
                        credentials[TokenCacheExtensions.AccessTokenKey] = "SampleToken";
                        credentials[TokenCacheExtensions.RefreshTokenKey] = "RefreshToken";
                        credentials["Expiry"] = DateTime.Now.AddMinutes(5).ToString("g");
                        return ValueTask.FromResult<IDictionary<string, string>?>(credentials);
                    }

                    // Return null/default to fail the LoginAsync method
                    return ValueTask.FromResult<IDictionary<string, string>?>(default);
                })
                .Refresh((sp, tokenDictionary, cancellationToken) =>
                {
                    // TODO: Write code to refresh tokens using the currently stored tokens
                    if ((tokenDictionary?.TryGetValue(TokenCacheExtensions.RefreshTokenKey, out var refreshToken) ?? false) &&
                        !refreshToken.IsNullOrEmpty() &&
                        (tokenDictionary?.TryGetValue("Expiry", out var expiry) ?? false) &&
                        DateTime.TryParse(expiry, out var tokenExpiry) &&
                        tokenExpiry > DateTime.Now)
                    {
                        // Return IDictionary containing any tokens used by service calls or in the app
                        tokenDictionary ??= new Dictionary<string, string>();
                        tokenDictionary[TokenCacheExtensions.AccessTokenKey] = "NewSampleToken";
                        tokenDictionary["Expiry"] = DateTime.Now.AddMinutes(5).ToString("g");
                        return ValueTask.FromResult<IDictionary<string, string>?>(tokenDictionary);
                    }

                    // Return null/default to fail the Refresh method
                    return ValueTask.FromResult<IDictionary<string, string>?>(default);
                }), name: "CustomAuth")
#endif
                )
#endif
                .ConfigureServices((context, services) =>
                {
                    // TODO: Register your services
                    //services.AddSingleton<IMyService, MyService>();
                })
#if (useReactiveExtensionsNavigation)
                .UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
#elif (useExtensionsNavigation)
                .UseNavigation(RegisterRoutes)
#endif
            );
        MainWindow = builder.Window;

//-:cnd:noEmit
#if DEBUG
        MainWindow.EnableHotReload();
#endif
//+:cnd:noEmit
        MainWindow.SetWindowIcon();

#if useFrameNav
//-:cnd:noEmit
        Host = builder.Build();

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

//+:cnd:noEmit
#if (!enableDeveloperMode)
            // Place the frame in the current Window
            MainWindow.Content = rootFrame;
#else
$$EnableDeveloperMode_Frame_MainWindowContent$$
#endif
//-:cnd:noEmit
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }
        // Ensure the current window is active
        MainWindow.Activate();
//+:cnd:noEmit
#elif (!useAuthentication)
#if (!enableDeveloperMode)
        Host = await builder.NavigateAsync<Shell>();
#else
$$EnableDeveloperMode_Region_Navigate$$
            ();
#endif
#else
#if (!enableDeveloperMode)
        Host = await builder.NavigateAsync<Shell>
#else
$$EnableDeveloperMode_Region_Navigate$$
#endif
            (initialNavigate: async (services, navigator) =>
            {
                var auth = services.GetRequiredService<IAuthenticationService>();
                var authenticated = await auth.RefreshAsync();
                if (authenticated)
                {
                    await navigator.NavigateViewModelAsync<$mainRouteViewModel$>(this, qualifier: Qualifiers.Nested);
                }
                else
                {
                    await navigator.NavigateViewModelAsync<$loginRouteViewModel$>(this, qualifier: Qualifiers.Nested);
                }
            });
#endif
    }
#if (useExtensionsNavigation)

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
#if (useRegionsNav)
        views.Register(
            new ViewMap(ViewModel: typeof($shellRouteViewModel$)),
#if (useAuthentication)
            new ViewMap<LoginPage, $loginRouteViewModel$>(),
#endif
            new ViewMap<MainPage, $mainRouteViewModel$>(),
            new DataViewMap<SecondPage, $secondRouteViewModel$, Entity>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<$shellRouteViewModel$>(),
                Nested:
                [
#if (useAuthentication)
                    new ("Login", View: views.FindByViewModel<$loginRouteViewModel$>()),
#endif
                    new ("Main", View: views.FindByViewModel<$mainRouteViewModel$>()),
                    new ("Second", View: views.FindByViewModel<$secondRouteViewModel$>()),
                ]
            )
        );
#endif
    }
#endif
}
