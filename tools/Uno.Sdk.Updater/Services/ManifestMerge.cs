#nullable enable
using Uno.Sdk.Models;

namespace Uno.Sdk.Services;

/// <summary>
/// Merges the packages.json shipped in Uno.Sdk.Private (the SDK manifest) with the local one
/// in this repository before the groups are updated and written out.
/// </summary>
/// <remarks>
/// In both modes the SDK owns <see cref="ManifestGroup.VersionProperty"/>: it is the name of the
/// MSBuild property that the Uno.Sdk code reads for that group, so a local copy can only be stale.
/// Tools outside this repository (the canary NuGet updater) read it from the published Uno.Sdk.
/// </remarks>
internal static class ManifestMerge
{
    /// <summary>
    /// Release Uno: keeps every local group as is (versions included) and adds groups that only
    /// exist in the SDK manifest.
    /// </summary>
    public static IEnumerable<ManifestGroup> MergeLocalManifest(IEnumerable<ManifestGroup> sdkManifest, IEnumerable<ManifestGroup> localManifest)
    {
        var sdkGroups = sdkManifest.ToList();
        var mergedManifest = localManifest
            .Select(local => WithSdkVersionProperty(local, sdkGroups.FirstOrDefault(x => x.Group == local.Group)))
            .ToList();

        foreach (var group in sdkGroups)
        {
            if (mergedManifest.Any(x => x.Group == group.Group))
            {
                continue;
            }

            mergedManifest.Add(group);
        }

        return mergedManifest;
    }

    /// <summary>
    /// Preview Uno: keeps local versions as the baseline (no downgrades), merges local
    /// VersionOverride entries over the SDK ones, takes the package list from the SDK and adds
    /// groups that only exist in the SDK manifest.
    /// </summary>
    public static IEnumerable<ManifestGroup> MergeLocalOverridesOnly(IEnumerable<ManifestGroup> sdkManifest, IEnumerable<ManifestGroup> localManifest)
    {
        var local = localManifest.ToList();

        // Start from LOCAL manifest -> prevents downgrades of existing groups
        var map = local.ToDictionary(g => g.Group, g => g, StringComparer.OrdinalIgnoreCase);

        // Keep local versions as the baseline to prevent downgrades.
        // For groups that have local VersionOverride entries, merge them over the
        // existing overrides in the map (local wins). This preserves any SDK overrides
        // that local did not specify, and ensures a case-insensitive dictionary.
        foreach (var lg in local.Where(g => g.VersionOverride is { Count: > 0 }))
        {
            if (!map.TryGetValue(lg.Group, out var existing))
            {
                continue;
            }

            // Start from existing overrides so we don't drop SDK-provided keys
            var merged = existing.VersionOverride is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(existing.VersionOverride, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in lg.VersionOverride!)
            {
                // Local overrides win (overwrite)
                merged[kv.Key] = kv.Value;
            }

            map[lg.Group] = existing with { VersionOverride = merged };
        }

        foreach (var sg in sdkManifest)
        {
            // New groups (in the SDK only) are added as is. For existing ones, the SDK owns which
            // packages belong to the group: a stale local list misses added packages (restore falls
            // back to nuget.org) and keeps moved ones (duplicate entries). It also owns the group's
            // version property.
            map[sg.Group] = map.TryGetValue(sg.Group, out var existing)
                ? WithSdkVersionProperty(existing with { Packages = sg.Packages }, sg)
                : sg;
        }

        return map.Values;
    }

    // An SDK manifest without the field (older Uno.Sdk.Private) leaves the local value untouched.
    private static ManifestGroup WithSdkVersionProperty(ManifestGroup local, ManifestGroup? sdk)
        => sdk?.VersionProperty is { Length: > 0 } property && property != local.VersionProperty
            ? local with { VersionProperty = property }
            : local;
}
