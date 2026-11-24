//+:cnd:noEmit
#pragma warning disable UNOASPIRE001 // The Uno.Extensions.Aspire.Hosting API is experimental.
var builder = DistributedApplication.CreateBuilder(args);

#if (useAspireServer)
var api = builder.AddDotnetProject("api", "../MyExtensionsApp.1.Server/MyExtensionsApp.1.Server.csproj");

#endif
// See https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Aspire/HowTo-Aspire.html
var app = builder.AddUnoApp("app", "../MyExtensionsApp.1/MyExtensionsApp.1.csproj");
#if (useDesktop)

#if (useAspireServer)
app.AddDesktop()
    .WithReference(api)
    .WaitFor(api);
#else
app.AddDesktop();
#endif
#endif
#if (useWasm)

// Start the WebAssembly head from the dashboard.
#if (useAspireServer)
var wasm = app.AddWebAssembly()
    .WithReference(api)
    .WaitFor(api);

// The browser calls the API from the WebAssembly head's origin.
api.WithEnvironment("Cors__Origin", wasm.GetEndpoint("http"));
#else
app.AddWebAssembly();
#endif
#endif

builder.Build().Run();
