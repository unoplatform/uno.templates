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

internal class Program
{
    [STAThread]
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
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
