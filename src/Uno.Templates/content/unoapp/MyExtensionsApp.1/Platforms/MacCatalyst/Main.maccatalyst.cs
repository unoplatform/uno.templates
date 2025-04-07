using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace MyExtensionsApp._1.MacCatalyst;

public class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
//+:cnd:noEmit
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
//-:cnd:noEmit
//+:cnd:noEmit
#if (!useSkiaRenderer)
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(App));
#else
		var host = new AppleUIKitHost(() => new App());
		host.Run();
#endif
//-:cnd:noEmit
    }
}
