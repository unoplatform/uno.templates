using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace MyExtensionsApp._1;

public class Program
{
    private static App? _app;

//+:cnd:noEmit
#if (!useSkiaRenderer)
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
#if (!useSkiaRenderer)
        Microsoft.UI.Xaml.Application.Start(_ => _app = new App());
#else
		var host = new WebAssemblyBrowserHost(() => _app = new App());
		await host.Run();
#endif
//-:cnd:noEmit

        return 0;
    }
}
