//-:cnd:noEmit
namespace MyExtensionsApp._1.MauiControls;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder)
    {
        builder.UseMauiCommunityToolkit();
        return builder;
    }
}