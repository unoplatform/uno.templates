namespace MyExtensionsApp._1;

public class Program
{
    private static App? _app;

//+:cnd:noEmit
#if (!skiaeverywhere)
    public static int Main(string[] args)
#else
    public static async Task<int> Main(string[] args)
#endif
//-:cnd:noEmit
    {
//+:cnd:noEmit
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
//-:cnd:noEmit
//+:cnd:noEmit
#if (!skiaeverywhere)
        Microsoft.UI.Xaml.Application.Start(_ => _app = new App());
#else
		var host = new global::Uno.UI.Runtime.Skia.WebAssembly.Browser.PlatformHost(() => _app = new App());
		await host.Run();
#endif
//-:cnd:noEmit

        return 0;
    }
}
