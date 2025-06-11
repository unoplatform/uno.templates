using Uno.UI.Hosting;
using MyExtensionsApp._1;
//+:cnd:noEmit
#if (mauiEmbedding)
using App = MyExtensionsApp._1.App;
#endif
//-:cnd:noEmit
internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
//+:cnd:noEmit
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();
#endif
//-:cnd:noEmit

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
