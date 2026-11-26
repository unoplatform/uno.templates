using System.Text.RegularExpressions;
using Uno.Sdk.Models;

namespace Uno.Sdk.Updater.Utils;

internal static class TargetFrameworkOverride
{
    // The highest major a target framework override can move to: "net10.0" -> 10, for the packages released
    // with each .NET version. A pin already above that major has its own numbering, so it gets no cap: it would
    // otherwise never update. Null (no cap) too for keys that aren't a .NET target framework.
    internal static int? GetMaxMajor(string key, NuGetVersion pinnedVersion)
    {
        var match = Regex.Match(key, @"^net(\d+)\.\d+");
        if (!match.Success)
        {
            return null;
        }

        var targetFrameworkMajor = int.Parse(match.Groups[1].Value);
        return pinnedVersion.Version.Major <= targetFrameworkMajor ? targetFrameworkMajor : null;
    }
}
