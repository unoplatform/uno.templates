using Uno.Extensions.Serialization;
using Windows.Networking.Connectivity;
using Windows.Storage;

namespace MyExtensionsApp._1.Services.Caching;

public sealed class WeatherCache : IWeatherCache
{
	private readonly IApiClient _api;
	private readonly ISerializer _serializer;
//+:cnd:noEmit
#if (useLogging)
	private readonly ILogger _logger;

	public WeatherCache(IApiClient api, ISerializer serializer, ILogger<WeatherCache> logger)
	{
		_api = api;
		_serializer = serializer;
		_logger = logger;
	}
#else
	public WeatherCache(IApiClient api, ISerializer serializer)
	{
		_api = api;
		_serializer = serializer;
	}
#endif

	private bool IsConnected => NetworkInformation.GetInternetConnectionProfile().GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;

	public async ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token)
	{
		var weatherText = await GetCachedWeather();
		if (!string.IsNullOrWhiteSpace(weatherText))
		{
			return _serializer.FromString<ImmutableArray<WeatherForecast>>(weatherText);
		}

		if(!IsConnected)
		{
#if (useLogging)
			_logger.LogWarning("App is offline and cannot connect to the API.");
#endif
			throw new Exception("No internet connection");
		}

		var response = await _api.GetWeather(token);

		if (response.IsSuccessStatusCode && response.Content is not null)
		{
			var weather = response.Content;
			await Save(weather, token);
			return weather;
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
			return ImmutableArray<WeatherForecast>.Empty;
		}
	}

	private async ValueTask<StorageFile> GetFile(CreationCollisionOption option) =>
		await ApplicationData.Current.TemporaryFolder.CreateFileAsync("weather.json", option);

	private async ValueTask<string?> GetCachedWeather()
	{
		var file = await GetFile(CreationCollisionOption.OpenIfExists);
		var properties = await file.GetBasicPropertiesAsync();

		// Reuse latest cache file if offline
		// or if the file is less than 5 minutes old
		if(IsConnected || DateTimeOffset.Now.AddMinutes(-5) > properties.DateModified)
		{
			return null;
		}

		return await File.ReadAllTextAsync(file.Path);
	}

	private async ValueTask Save(IImmutableList<WeatherForecast> weather, CancellationToken token)
	{
		var weatherText = _serializer.ToString(weather);
		var file = await GetFile(CreationCollisionOption.ReplaceExisting);
		await File.WriteAllTextAsync(file.Path, weatherText);
	}
}
