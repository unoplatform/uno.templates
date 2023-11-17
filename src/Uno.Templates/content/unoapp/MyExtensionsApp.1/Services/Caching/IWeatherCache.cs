//-:cnd:noEmit
namespace MyExtensionsApp._1.Services.Caching;

public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
