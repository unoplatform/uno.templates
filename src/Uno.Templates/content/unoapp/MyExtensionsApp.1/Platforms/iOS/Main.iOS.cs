using UIKit;
using Uno.UI.Hosting;
using MyExtensionsApp._1;

//+:cnd:noEmit
#if (!useDependencyInjection && useLoggingFallback)
App.InitializeLogging();

#endif
//-:cnd:noEmit
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseAppleUIKit()
    .Build();

host.Run();
