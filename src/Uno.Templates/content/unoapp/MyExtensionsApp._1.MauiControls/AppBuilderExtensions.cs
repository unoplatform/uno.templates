//-:cnd:noEmit
namespace MyExtensionsApp._1;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) =>
		builder
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("MyExtensionsApp._1/Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
				fonts.AddFont("MyExtensionsApp._1/Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
			});
}