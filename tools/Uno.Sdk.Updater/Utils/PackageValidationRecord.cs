#nullable enable
using Uno.Sdk.Models;

namespace Uno.Sdk.Updater.Utils;

internal sealed class PackageValidationRecord
{
    private readonly Dictionary<string, List<VersionValidationResult>> _validated = [];

    internal bool HasBeenChecked(string packageId, NuGetVersion version) =>
        GetResult(packageId, version) is not null;

    internal bool IsValid(string packageId, NuGetVersion version) =>
        GetResult(packageId, version)?.IsValid ?? throw new InvalidOperationException($"Package {packageId} - {version}, has not been checked");

    internal void AddResult(string packageId, NuGetVersion version, bool validated)
    {
        if (HasBeenChecked(packageId, version))
        {
            return;
        }
        else if (!_validated.ContainsKey(packageId))
        {
            _validated[packageId] = [];
        }

        if (!validated)
        {
            Console.WriteLine($"{packageId} {version} is not a valid dependency.");
        }

        _validated[packageId].Add(new VersionValidationResult(version, validated));
    }

    private VersionValidationResult? GetResult(string packageId, NuGetVersion version) =>
        _validated.TryGetValue(packageId, out List<VersionValidationResult>? value) ? value.FirstOrDefault(x => x.Version == version) : null;

    private record VersionValidationResult(NuGetVersion Version, bool IsValid);
}
