using UIKit;
using Uno.UI.Hosting;
//+:cnd:noEmit
#if (useWebGpuRenderer)
using Uno.UI.Composition.WebGpu;
#endif
#if (useWebGpuRenderer && useSkiaRenderer)
using Uno.UI.Composition.Skia;
#endif
//-:cnd:noEmit

namespace MyExtensionsApp._1.iOS;

public class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
//+:cnd:noEmit
#if (useWebGpuRenderer)
            .GraphicsBackend(WebGpuBackend.CreateGraphicsProvider())
#endif
#if (useWebGpuRenderer && useSkiaRenderer)
            .GraphicsBackend(SkiaBackend.CreateGraphicsProvider())
#endif
//-:cnd:noEmit
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}
