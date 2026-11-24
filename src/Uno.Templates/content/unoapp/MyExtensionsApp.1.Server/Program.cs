//+:cnd:noEmit
#if (useHttp && useServer)
using System.Text.Json.Serialization.Metadata;
#endif
#if (useSerilog)
using Serilog;
#endif
#if (useSerilog && useAspireServer)
using Serilog.Extensions.Logging;
#endif
#if (useWasm)
using Uno.Wasm.Bootstrap.Server;
#endif
#if (useHttp && useServer)
using MyExtensionsApp._1.DataContracts.Serialization;
#endif

try
{
#if (useSerilog)
#if (useAspireServer)
    // Serilog replaces the logging providers: forwarding to them sends the logs to the Aspire dashboard too.
    var providers = new LoggerProviderCollection();
#endif
    Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine("App_Data", "Logs", "log.txt"))
#if (useAspireServer)
            .WriteTo.Providers(providers)
#endif
            .CreateLogger();
#endif
    var builder = WebApplication.CreateBuilder(args);
#if (useSerilog && useAspireServer)
    SerilogHostBuilderExtensions.UseSerilog(builder.Host, providers: providers);
#elif (useSerilog)
    SerilogHostBuilderExtensions.UseSerilog(builder.Host);
#endif
#if (useAspireServer)
    // Telemetry and health checks for the Aspire dashboard
    builder.AddServiceDefaults();
#if (useWasm)
    builder.Services.AddCors();
#endif
#endif

#if (useHttp && useServer)
    // Configure the JsonOptions to use the generated WeatherForecastContext
    builder.Services.Configure<JsonOptions>(options =>
        options.JsonSerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(
            WeatherForecastContext.Default
        ));
#endif
    // Configure the RouteOptions to use lowercase URLs
    builder.Services.Configure<RouteOptions>(options =>
        options.LowercaseUrls = true);

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        // Include XML comments for all included assemblies
        Directory.EnumerateFiles(AppContext.BaseDirectory, "*.xml")
            .Where(x => x.Contains("MyExtensionsApp._1")
                && File.Exists(Path.Combine(
                    AppContext.BaseDirectory,
                    $"{Path.GetFileNameWithoutExtension(x)}.dll")))
            .ToList()
            .ForEach(path => c.IncludeXmlComments(path));
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
#if (useAspireServer && useWasm)

    // The AppHost sets the origin of its WebAssembly head; without it, no cross-origin call is allowed.
    if (app.Configuration["Cors:Origin"] is { } origin)
    {
        app.UseCors(policy => policy.WithOrigins(origin).AllowAnyHeader().AllowAnyMethod());
    }
#endif

    app.UseHttpsRedirection();
#if useWasm

    app.UseUnoFrameworkFiles();
    app.MapFallbackToFile("index.html");
#endif

#if useServer
    app.MapWeatherApi();
#endif
#if (useAspireServer)
    app.MapDefaultEndpoints();
#endif
    app.UseStaticFiles();

    await app.RunAsync();
}
catch (Exception ex)
{
#if (useSerilog)
    Log.Fatal(ex, "Application terminated unexpectedly");
#else
    Console.Error.WriteLine("Application terminated unexpectedly");
    Console.Error.WriteLine(ex);
#endif
//-:cnd:noEmit
#if DEBUG
    if (System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Break();
    }
#endif
//+:cnd:noEmit
}
#if (useSerilog)
finally
{
    Log.CloseAndFlush();
}
#endif
//-:cnd:noEmit
