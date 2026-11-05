using Uno.UI.Hosting;
//+:cnd:noEmit
#if (useWebGpuRenderer)
using Uno.UI.Composition.WebGpu;
#endif
#if (useWebGpuRenderer && useSkiaRenderer)
using Uno.UI.Composition.Skia;
#endif
//-:cnd:noEmit

namespace MyExtensionsApp._1;

public class Program
{
    public static async Task Main(string[] args)
    {
//+:cnd:noEmit
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
//-:cnd:noEmit
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
//+:cnd:noEmit
#if (skiaBeforeWebGpu)
            .GraphicsBackend(SkiaBackend.CreateGraphicsProvider())
#endif
#if (useWebGpuRenderer)
            .GraphicsBackend(WebGpuBackend.CreateGraphicsProvider())
#endif
#if (webGpuBeforeSkia)
            .GraphicsBackend(SkiaBackend.CreateGraphicsProvider())
#endif
//-:cnd:noEmit
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}