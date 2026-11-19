using Uno.Sdk.Models;
using Uno.Sdk.Services;

namespace Uno.Sdk.Updater.Config;

// HarfBuzzSharp ships with each SkiaSharp release: all the packages of one release are built from one commit of the
// SkiaSharp repository, and each nuspec records it (<repository commit="...">). Both groups move by hand (see
// .github/sdk-update-exclude.json), so nothing else tells when a SkiaSharp move forgot HarfBuzzSharp, or the reverse.
internal static class SkiaSharpRelease
{
    internal const string SkiaSharpGroup = "SkiaSharp";
    internal const string HarfBuzzSharpGroup = "HarfBuzzSharp";

    // Why the manifest's HarfBuzzSharp pin isn't from its SkiaSharp pin's release, or null when it is.
    internal static async Task<string?> FindHarfBuzzSharpMismatchAsync(IEnumerable<ManifestGroup> manifest, NuGetApiClient client)
    {
        var groups = manifest.ToDictionary(g => g.Group);
        if (!groups.TryGetValue(SkiaSharpGroup, out var skiaSharp) || !groups.TryGetValue(HarfBuzzSharpGroup, out var harfBuzzSharp))
        {
            return $"The manifest needs both the '{SkiaSharpGroup}' and '{HarfBuzzSharpGroup}' groups.";
        }

        // Every package of a group ships at the group's version, so its first package gives the release's commit.
        var skiaSharpPackage = $"{skiaSharp.Packages[0]} {skiaSharp.Version}";
        var harfBuzzSharpPackage = $"{harfBuzzSharp.Packages[0]} {harfBuzzSharp.Version}";
        var skiaSharpCommit = await client.GetRepositoryCommitAsync(skiaSharp.Packages[0], skiaSharp.Version);
        var harfBuzzSharpCommit = await client.GetRepositoryCommitAsync(harfBuzzSharp.Packages[0], harfBuzzSharp.Version);

        if (skiaSharpCommit is null || harfBuzzSharpCommit is null)
        {
            return $"Could not read the source commit of {(skiaSharpCommit is null ? skiaSharpPackage : harfBuzzSharpPackage)} on nuget.org.";
        }

        return string.Equals(skiaSharpCommit, harfBuzzSharpCommit, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"{harfBuzzSharpPackage} (commit {harfBuzzSharpCommit}) is not from the release of {skiaSharpPackage} (commit {skiaSharpCommit}): "
                + "pin the HarfBuzzSharp version that SkiaSharp release ships.";
    }
}
