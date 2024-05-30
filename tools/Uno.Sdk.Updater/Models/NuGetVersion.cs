using System.Text.RegularExpressions;

namespace Uno.Sdk.Models;

internal readonly struct NuGetVersion : IComparable<NuGetVersion>
{
    private NuGetVersion(string originalVersion, Version version, string? previewTag)
    {
        OriginalVersion = originalVersion;
        PreviewTag = previewTag;
        IsPreview = originalVersion.Contains('-') || originalVersion.Contains("255.255.255.255");
        Version = version;
    }

    public bool IsPreview { get; }

    public Version Version { get; }
    public string? PreviewTag { get; }

    public string OriginalVersion { get; }

    public override string ToString() => OriginalVersion;

    public static NuGetVersion Parse(string originalVersion) =>
        TryParse(originalVersion, out var nugetVersion)
            ? nugetVersion
            : throw new FormatException("Invalid version format");

    public static bool TryParse(string? originalVersion, out NuGetVersion nugetVersion)
    {
        nugetVersion = default;
        if (originalVersion is null || string.IsNullOrEmpty(originalVersion))
        {
            return false;
        }

        try
        {
            var match = Regex.Match(originalVersion, @"^(?<version>\d+\.\d+(\.\d+)?(\.\d+)?)(-(?<preview>[a-zA-Z0-9\-.]+))?$");
            if (match.Success)
            {
                var versionString = match.Groups["version"].Value;
                var previewTag = match.Groups["preview"].Success ? match.Groups["preview"].Value : null;
                if (!string.IsNullOrEmpty(versionString))
                {
                    nugetVersion = new NuGetVersion(originalVersion, new Version(versionString), previewTag);
                    return true;
                }
            }
        }
        catch
        {
            // Suppress any exceptions
        }

        return false;
    }

    public int CompareTo(NuGetVersion other)
    {
        var versionComparison = Version.CompareTo(other.Version);
        if (versionComparison != 0)
        {
            return versionComparison;
        }

        // If both are previews or both are stable, maintain their order (consider them equal in terms of sorting)
        if (IsPreview && other.IsPreview)
        {
            return ComparePreviewTags(PreviewTag, other.PreviewTag);
        }

        if (IsPreview)
        {
            return 1; // This instance is a preview, and should come after a non-preview
        }

        if (other.IsPreview)
        {
            return -1; // The other instance is a preview, and should come after a non-preview
        }

        return 0; // Both are non-preview, and versions are equal
    }

    private static int ComparePreviewTags(string? preview1, string? preview2)
    {
        if (preview1 == null && preview2 == null) return 0;
        if (preview1 == null) return -1;
        if (preview2 == null) return 1;

        char[] separators = ['.', '-'];
        var parts1 = preview1.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var parts2 = preview2.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var length = Math.Min(parts1.Length, parts2.Length);

        for (int i = 0; i < length; i++)
        {
            if (int.TryParse(parts1[i], out var num1) && int.TryParse(parts2[i], out var num2))
            {
                var comparison = num1.CompareTo(num2);
                if (comparison != 0) return comparison;
            }
            else
            {
                var comparison = string.Compare(parts1[i], parts2[i], StringComparison.Ordinal);
                if (comparison != 0) return comparison;
            }
        }

        return parts1.Length.CompareTo(parts2.Length);
    }

    public static bool operator <(NuGetVersion left, NuGetVersion right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(NuGetVersion left, NuGetVersion right) =>
        left.CompareTo(right) > 0;

    public static bool operator <=(NuGetVersion left, NuGetVersion right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >=(NuGetVersion left, NuGetVersion right) =>
        left.CompareTo(right) >= 0;

    public static implicit operator NuGetVersion(string version) => Parse(version);

    public static implicit operator string(NuGetVersion version) => version.OriginalVersion;
}
