using System.Text.RegularExpressions;

namespace TemplateTfmSwitchGenerator;

/// <summary>
/// Aligns hand-written Windows TFMs in the template content with
/// <see cref="TemplateTfms.WindowsPlatformVersion"/>.
/// </summary>
public static partial class WindowsTfmRewriter
{
    public static string Rewrite(string content, string platformVersion) =>
        WindowsTfm().Replace(content, match => $"-windows{platformVersion}{match.Groups["revision"].Value}");

    // Matches the platform version of a Windows TFM, e.g. "-windows10.0.26100" or
    // "-windows10.0.26100.0" - the optional revision is preserved as authored.
    [GeneratedRegex(@"-windows\d+\.\d+\.\d+(?<revision>\.\d+)?")]
    private static partial Regex WindowsTfm();
}
