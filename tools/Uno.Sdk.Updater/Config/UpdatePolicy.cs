using Uno.Sdk.Models;

namespace Uno.Sdk.Updater.Config;

// Which manifest groups the updater holds back, and how far it may move the others.
internal static class UpdatePolicy
{
    // All the MAUI packages ship together at one version. Microsoft.Maui.Controls.Compatibility has its own group
    // only because it has no .NET 11 build (see PackageManifest.cs in uno), so both groups follow the same rules:
    // moving one without the other mixes MAUI versions in an app (Controls.Core and Controls.Xaml from one,
    // Controls.Build.Tasks and Resizetizer from the other).
    internal static readonly string[] MauiGroups = ["Maui", "MauiCompatibility"];

    // Those groups have major versions update disabled
    // as they are generally containing breaking changes.
    internal static readonly string[] MajorUpgradeDisabledGroups = ["SkiaSharp", "WasmBootstrap", "MicrosoftLoggingConsole", .. MauiGroups];

    // Groups the updater leaves as they are, to avoid Java misalignment:
    // - AndroidX packages, on every branch
    // - MAUI, on release branches (a stable Uno version), as a new MAUI also moves its AndroidX dependencies
    internal static bool IsHeldToAvoidJavaMisalignment(ManifestGroup group, bool isPreviewUno)
        => group.Packages.Any(x => x.StartsWith("Xamarin"))
            || (!isPreviewUno && MauiGroups.Contains(group.Group));
}
