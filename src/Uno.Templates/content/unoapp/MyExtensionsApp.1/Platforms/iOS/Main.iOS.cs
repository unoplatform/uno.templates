using UIKit;
using Uno.UI.Hosting;
using MyExtensionsApp._1;
//+:cnd:noEmit
#if (mauiEmbedding)
using App = MyExtensionsApp._1.App;
#endif
//-:cnd:noEmit

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
