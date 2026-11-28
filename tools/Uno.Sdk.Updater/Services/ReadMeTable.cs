#nullable enable
using System.Text;
using Uno.Sdk.Models;

namespace Uno.Sdk.Services;

/// <summary>
/// Writes the "MSBuild Property | Default Version" table of the Uno.Sdk ReadMe from the
/// <see cref="ManifestGroup.VersionProperty"/> of each group, so a property added to the Uno.Sdk
/// shows up in the ReadMe without editing the template.
/// </summary>
internal static class ReadMeTable
{
    public const string StartMarker = "<!-- VersionProperties:Start -->";
    public const string EndMarker = "<!-- VersionProperties:End -->";

    /// <summary>
    /// Replaces the table between <see cref="StartMarker"/> and <see cref="EndMarker"/>.
    /// The ReadMe is returned unchanged when the markers are missing or when no group has a
    /// version property (an Uno.Sdk.Private that predates the field): the hand-written table
    /// in the template is then kept.
    /// </summary>
    public static string ReplaceVersionPropertiesTable(string readMe, IEnumerable<ManifestGroup> manifest)
    {
        var groups = manifest.ToList();
        var start = readMe.IndexOf(StartMarker, StringComparison.Ordinal);
        var end = readMe.IndexOf(EndMarker, StringComparison.Ordinal);

        if (start < 0 || end < start || !groups.Any(g => !string.IsNullOrWhiteSpace(g.VersionProperty)))
        {
            return readMe;
        }

        var newLine = readMe.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        // Blank lines around the table: it must not touch the HTML comments to render as a table.
        var table = new StringBuilder()
            .Append(newLine)
            .Append(newLine)
            .Append("| MSBuild Property | Default Version |").Append(newLine)
            .Append("|----------------|:---------------:|").Append(newLine);

        // UnoVersion comes from the Uno.Sdk version itself, not from a property (first footnote).
        if (groups.FirstOrDefault(g => g.Group == "Core") is { } core)
        {
            table.Append("| UnoVersion* | ").Append(core.Version).Append(" |").Append(newLine);
        }

        // A property can drive several groups (MauiVersion); list it once, with its first group.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups.Where(g => !string.IsNullOrWhiteSpace(g.VersionProperty)))
        {
            if (!seen.Add(group.VersionProperty!))
            {
                continue;
            }

            // "**": the version differs for some target framework (second footnote).
            var mark = group.VersionOverride is { Count: > 0 } ? "**" : string.Empty;
            table.Append("| ").Append(group.VersionProperty).Append(mark).Append(" | ").Append(group.Version).Append(" |").Append(newLine);
        }

        table.Append(newLine);

        return readMe[..(start + StartMarker.Length)] + table + readMe[end..];
    }
}
