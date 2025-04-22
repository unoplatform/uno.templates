//-:cnd:noEmit
using System.Net;

namespace MyExtensionsApp._1.Services.Caching;

public sealed class WeatherCache : IWeatherCache
{
#if (useHttpKiota)
    private readonly MyExtensionsApp._1.Client.WeatherServiceClient _client;
#else
    private readonly IApiClient _api;
#endif
    private readonly ISerializer _serializer;
//+:cnd:noEmit
#if (useLogging)
    private readonly ILogger _logger;
#endif

    public WeatherCache(
#if (useHttpKiota)
        MyExtensionsApp._1.Client.WeatherServiceClient client,
#else
        IApiClient api,
#endif
        ISerializer serializer
#if (useLogging)
        , ILogger<WeatherCache> logger
#endif
    )
    {
#if (useHttpKiota)
        _client = client;
#else
        _api = api;
#endif
        _serializer = serializer;
#if (useLogging)
        _logger = logger;
#endif
    }

    private bool IsConnected => NetworkInformation.GetInternetConnectionProfile().GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;

    public async ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token)
    {
        var weatherText = await GetCachedWeather(token);
        if (!string.IsNullOrWhiteSpace(weatherText))
        {
            return _serializer.FromString<ImmutableArray<WeatherForecast>>(weatherText);
        }

        if (!IsConnected)
        {
#if (useLogging)
            _logger.LogWarning("App is offline and cannot connect to the API.");
#endif
            throw new WebException("No internet connection", WebExceptionStatus.ConnectFailure);
        }
        
        IImmutableList<WeatherForecast> weather;

#if (useHttpKiota)
        var response = await _client
            .Api
            .Weatherforecast
            .GetAsync(null, token)
            .ConfigureAwait(false)
            ?? new List<global::MyExtensionsApp._1.Client.Models.WeatherForecast>();

        var json    = _serializer.ToString(response);
        weather = _serializer.FromString<ImmutableArray<WeatherForecast>>(json);
#else
        var response = await _api.GetWeather(token);

        if (response.IsSuccessStatusCode && response.Content is not null)
        {
            weather = response.Content;
        }
        else if (response.Error is not null)
        {
#if (useLogging)
            _logger.LogError(response.Error, "An error occurred while retrieving the latest Forecast.");
#endif
            throw response.Error;
        }
        else
        {
            weather = ImmutableArray<WeatherForecast>.Empty;
        }
#endif

        await Save(weather, token);
        return weather;
    }

    private async ValueTask<string?> GetCachedWeather(CancellationToken token)
    {
        var file = await GetFile(CreationCollisionOption.OpenIfExists);
        var properties = await file.GetBasicPropertiesAsync();

        // Reuse latest cache file if offline
        // or if the file is less than 5 minutes old
        if (IsConnected || DateTimeOffset.Now.AddMinutes(-5) > properties.DateModified || token.IsCancellationRequested)
        {
            return null;
        }

        return await File.ReadAllTextAsync(file.Path, token);
    }

    private async ValueTask Save(IImmutableList<WeatherForecast> weather, CancellationToken token)
    {
        var weatherText = _serializer.ToString(weather);
        var file = await GetFile(CreationCollisionOption.ReplaceExisting);
        await File.WriteAllTextAsync(file.Path, weatherText, token);
    }
}
