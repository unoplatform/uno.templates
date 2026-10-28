using Microsoft.UI.Reactor;
using MyReactorApp._1;

// Reactor owns the Uno Application, the window and the render loop, so this one call is the
// whole entry point on desktop, WebAssembly and iOS.
// Android has no console entry point: the OS starts the Application in Platforms/Android,
// whose CreateHost calls ReactorApp.CreateHost<MainComponent>() instead.
//-:cnd:noEmit
#if !__ANDROID__
ReactorApp.Run<MainComponent>("MyReactorApp.1", width: 480, height: 360);
#endif
//+:cnd:noEmit
