namespace MyExtensionsApp._1;

public class Program
{
    private static App? _app;

    public static int Main(string[] args)
    {
        //+:cnd:noEmit
#if (!useDependencyInjection && useLoggingFallback)
        App.InitializeLogging();

#endif
//-:cnd:noEmit
        Microsoft.UI.Xaml.Application.Start(_ => _app = new App());

        return 0;
    }
}
