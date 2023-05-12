//-:cnd:noEmit
namespace MyExtensionsApp._1;

public class App : Application
{
	public static Window? MainWindow { get; private set; }
	public static IHost? Host { get; private set; }

//+:cnd:noEmit
#if useFrameNav
	protected override void OnLaunched(LaunchActivatedEventArgs args)
#else
	protected async override void OnLaunched(LaunchActivatedEventArgs args)
#endif
	{
		var builder = this.CreateBuilder(args)
#if (useNavigationToolkit)
			// Add navigation support for toolkit controls such as TabBar and NavigationView
			.UseToolkitNavigation()
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
					logBuilder.SetMinimumLevel(
						context.HostingEnvironment.IsDevelopment() ?
							LogLevel.Information :
							LogLevel.Warning);
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
				.ConfigureServices((context, services) => {
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

#if useFrameNav
//-:cnd:noEmit
		Host = builder.Build();

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (MainWindow.Content is not Frame rootFrame)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new Frame();

			// Place the frame in the current Window
			MainWindow.Content = rootFrame;
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
		Host = await builder.NavigateAsync<Shell>();
#else
        Host = await builder.NavigateAsync<Shell>(initialNavigate:
            async (services, navigator) =>
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
				Nested: new RouteMap[]
				{
#if (useAuthentication)
					new RouteMap("Login", View: views.FindByViewModel<$loginRouteViewModel$>()),
#endif
					new RouteMap("Main", View: views.FindByViewModel<$mainRouteViewModel$>()),
					new RouteMap("Second", View: views.FindByViewModel<$secondRouteViewModel$>()),
				}
			)
		);
#endif
	}
#endif
}
