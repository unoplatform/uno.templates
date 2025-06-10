//-:cnd:noEmit
namespace MyExtensionsApp._1.Services.Caching;
//+:cnd:noEmit
#if (useHttpKiota)
using WeatherForecast = MyExtensionsApp._1.Client.Models.WeatherForecast;
#elif (useHttpRefit)
using WeatherForecast = MyExtensionsApp._1.DataContracts.WeatherForecast;
#endif
public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
