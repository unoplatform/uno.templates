namespace TemplateTfmSwitchGenerator;

/// <summary>
/// Single source of truth for the target frameworks the templates emit.
/// </summary>
public static class TemplateTfms
{
    /// <summary>
    /// Windows platform version appended to every Windows TFM the templates emit
    /// (both the generated switch cases and the hand-written mirrors).
    /// </summary>
    public const string WindowsPlatformVersion = "10.0.26100";

    public const string SingleProjectTfmsSymbol = "singleProjectTfms";

    public static string[] Runtimes { get; } = ["net9.0", "net10.0"];

    public static Platform[] Platforms { get; } =
    [
        new Platform("platforms == android", "platforms != android", "android"),
        new Platform("platforms == ios", "platforms != ios", "ios"),
        new Platform("platforms == windows", "platforms != windows", $"windows{WindowsPlatformVersion}"),
        new Platform("platforms == wasm", "platforms != wasm", "browserwasm"),
        new Platform("platforms == desktop", "platforms != desktop", "desktop"),
        new Platform("useUnitTests == true", "useUnitTests == false", null)
    ];
}
