using Uno.Sdk.Models;

namespace Uno.Sdk.Updater;

partial class LocalFileSystem
{
    static LocalFileSystem()
    {
        TemplateVersion = NuGetVersion.Parse(GitVersionInformation.NuGetVersion);

        var major = TemplateVersion.Version.Major;
        var minor = TemplateVersion.Version.Minor;

        if (TemplateVersion.IsPreview)
        {
            MaxVersion = NuGetVersion.Parse($"{major + 1}.0.0-dev.0");
            MinVersion = NuGetVersion.Parse($"{major}.{minor}.0-a.0");
        }
        else
        {
            MaxVersion = NuGetVersion.Parse($"{major}.{minor + 1}.0");
            MinVersion = NuGetVersion.Parse($"{major}.{minor}.0");
        }
    }

    public static readonly NuGetVersion TemplateVersion;

    public static readonly NuGetVersion MinVersion;

    public static readonly NuGetVersion MaxVersion;
}
