//+:cnd:noEmit
namespace MyExtensionsApp._1.DataContracts;

#if (includeNet6DataContractReferences)
/// <summary>
/// A Weather Forecast for a specific date
/// </summary>
public class WeatherForecast
{
	/// <summary>
	/// Gets the Date of the Forecast.
	/// </summary>
	public DateTime Date { get; set; }

	/// <summary>
	/// Gets the Forecast Temperature in Celsius.
	/// </summary>
	public double TemperatureC { get; set; }

	/// <summary>
	/// Get a description of how the weather will feel.
	/// </summary>
	public string? Summary { get; set; }

	/// <summary>
	/// Gets the Forecast Temperature in Fahrenheit
	/// </summary>
	public double TemperatureF => 32 + (TemperatureC * 9 / 5);
}
#else
/// <summary>
/// A Weather Forecast for a specific date
/// </summary>
/// <param name="Date">Gets the Date of the Forecast.</param>
/// <param name="TemperatureC">Gets the Forecast Temperature in Celsius.</param>
/// <param name="Summary">Get a description of how the weather will feel.</param>
public record WeatherForecast(DateOnly Date, double TemperatureC, string? Summary)
{
	/// <summary>
	/// Gets the Forecast Temperature in Fahrenheit
	/// </summary>
	public double TemperatureF => 32 + (TemperatureC * 9 / 5);
}
#endif
