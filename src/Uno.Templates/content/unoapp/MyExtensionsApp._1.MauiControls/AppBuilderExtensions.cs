//-:cnd:noEmit
using CommunityToolkit.Maui;

namespace MyExtensionsApp._1;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) =>
		builder.UseMauiCommunityToolkit();
}