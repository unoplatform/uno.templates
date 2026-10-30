using Microsoft.UI.Reactor;
using MyReactorApp._1;

// Reactor owns the Uno Application, the window and the render loop, so this one call is the
// whole entry point on every target.
// Android never calls Main on its own: Reactor adds the Android Application and Activity,
// and the Application runs this Main.
ReactorApp.Run<MainComponent>("MyReactorApp.1", width: 480, height: 360);
