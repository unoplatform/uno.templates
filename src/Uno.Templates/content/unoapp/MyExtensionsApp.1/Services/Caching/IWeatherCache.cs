//-:cnd:noEmit
namespace MyExtensionsApp._1.Services.Caching;
//+:cnd:noEmit
#if(useHttpKiota)
using WeatherForecast = test.Client.Models.WeatherForecast;
#elif (useHttpRefit)
using WeatherForecast = test.DataContracts.WeatherForecast;
#endif
public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
