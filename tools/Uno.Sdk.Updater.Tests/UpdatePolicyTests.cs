using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Models;
using Uno.Sdk.Services;
using Uno.Sdk.Updater.Config;

namespace Uno.Sdk.Updater.Tests;

[TestClass]
public class UpdatePolicyTests
{
    [TestMethod]
    [DataRow("Maui", "Microsoft.Maui.Controls", DisplayName = "Maui")]
    [DataRow("MauiCompatibility", "Microsoft.Maui.Controls.Compatibility", DisplayName = "MauiCompatibility")]
    public void MauiGroups_AreHeld_OnReleaseBranches(string group, string package)
    {
        Assert.IsTrue(UpdatePolicy.IsHeldToAvoidJavaMisalignment(Group(group, package), isPreviewUno: false));
    }

    [TestMethod]
    [DataRow("Maui", "Microsoft.Maui.Controls", DisplayName = "Maui")]
    [DataRow("MauiCompatibility", "Microsoft.Maui.Controls.Compatibility", DisplayName = "MauiCompatibility")]
    public void MauiGroups_AreUpdated_OnDevBranches(string group, string package)
    {
        Assert.IsFalse(UpdatePolicy.IsHeldToAvoidJavaMisalignment(Group(group, package), isPreviewUno: true));
    }

    [TestMethod]
    [DataRow(true, DisplayName = "Dev branch")]
    [DataRow(false, DisplayName = "Release branch")]
    public void AndroidXGroups_AreAlwaysHeld(bool isPreviewUno)
    {
        Assert.IsTrue(UpdatePolicy.IsHeldToAvoidJavaMisalignment(Group("AndroidXActivity", "Xamarin.AndroidX.Activity"), isPreviewUno));
    }

    [TestMethod]
    [DataRow(true, DisplayName = "Dev branch")]
    [DataRow(false, DisplayName = "Release branch")]
    public void Group_IsHeld_WhenAnyOfItsPackagesIsAndroidX(bool isPreviewUno)
    {
        var group = new ManifestGroup("AndroidXNavigation", "2.9.8.1", ["Some.Other.Package", "Xamarin.AndroidX.Navigation.UI"]);

        Assert.IsTrue(UpdatePolicy.IsHeldToAvoidJavaMisalignment(group, isPreviewUno));
    }

    [TestMethod]
    // Uno's own MAUI packages are Uno packages: they must keep moving on release branches.
    [DataRow("UnoMaui", "Uno.Maui.Embedding", DisplayName = "UnoMaui")]
    [DataRow("UnoRetarget", "Uno.WinUI.Retarget", DisplayName = "UnoRetarget")]
    [DataRow("Toolkit", "Uno.Toolkit.WinUI", DisplayName = "Toolkit")]
    [DataRow("SkiaSharp", "SkiaSharp.Skottie", DisplayName = "SkiaSharp")]
    [DataRow("maui", "Microsoft.Maui.Controls", DisplayName = "Group name match is exact")]
    public void OtherGroups_AreNotHeld_OnReleaseBranches(string group, string package)
    {
        Assert.IsFalse(UpdatePolicy.IsHeldToAvoidJavaMisalignment(Group(group, package), isPreviewUno: false));
    }

    [TestMethod]
    public void MauiGroups_FollowTheSameRules()
    {
        // The two groups are one MAUI version: a rule for one must apply to the other.
        CollectionAssert.IsSubsetOf(UpdatePolicy.MauiGroups, UpdatePolicy.MajorUpgradeDisabledGroups);
    }

    [TestMethod]
    public void Manifest_EveryMicrosoftMauiPackage_IsInAMauiGroup()
    {
        // A new group holding MAUI packages (another split) must join MauiGroups, or the updater moves it alone.
        var offenders = LoadManifest()
            .Where(g => g.Packages.Any(p => p.StartsWith("Microsoft.Maui.", StringComparison.Ordinal)))
            .Where(g => !UpdatePolicy.MauiGroups.Contains(g.Group))
            .Select(g => g.Group)
            .ToArray();

        Assert.AreEqual(0, offenders.Length, $"Groups with Microsoft.Maui.* packages outside UpdatePolicy.MauiGroups: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void Manifest_MauiGroups_PinTheSameDotNet10Version()
    {
        // Microsoft.Maui.Controls.Compatibility has no .NET 11 build, so its version is the .NET 10 MAUI version.
        var manifest = LoadManifest().ToDictionary(g => g.Group);
        const string MissingGroup = "The manifest no longer has the '{0}' group: update UpdatePolicy.MauiGroups and this test.";
        Assert.IsTrue(manifest.TryGetValue("Maui", out var maui), string.Format(MissingGroup, "Maui"));
        Assert.IsTrue(manifest.TryGetValue("MauiCompatibility", out var mauiCompatibility), string.Format(MissingGroup, "MauiCompatibility"));

        // Maui's base version is the .NET 10 MAUI one unless a net10.0 versionOverride pins it.
        var net10Override = maui!.VersionOverride?.GetValueOrDefault("net10.0");
        var mauiNet10 = net10Override ?? maui.Version;

        Assert.AreEqual(mauiNet10, mauiCompatibility!.Version,
            $"MauiCompatibility must pin the Maui group's .NET 10 version (its {(net10Override is null ? "base version" : "net10.0 versionOverride")}).");
    }

    [TestMethod]
    public void ExcludeList_HoldsHarfBuzzSharp_WithSkiaSharp()
    {
        // HarfBuzzSharp ships with each SkiaSharp release, and SkiaSharp moves by hand with Uno core.
        // Left to the updater, HarfBuzzSharp moves alone to the newest SkiaSharp release's.
        var excluded = LoadExcludeList();

        Assert.IsTrue(excluded.Contains("SkiaSharp"), "SkiaSharp is no longer in the exclude list: check HarfBuzzSharp too.");
        Assert.IsTrue(excluded.Contains("HarfBuzzSharp"), "HarfBuzzSharp must be excluded while SkiaSharp is: it ships with each SkiaSharp release.");
    }

    [TestMethod]
    public void Manifest_EverySkiaSharpReleaseGroup_IsExcluded()
    {
        // A new group holding SkiaSharp or HarfBuzzSharp packages (another split) must join the exclude list,
        // or the updater moves it alone, past the SkiaSharp release Uno core ships.
        var excluded = LoadExcludeList();
        var groups = LoadManifest()
            .Where(g => g.Packages.Any(p => p.StartsWith("SkiaSharp.", StringComparison.Ordinal) || p.StartsWith("HarfBuzzSharp.", StringComparison.Ordinal)))
            .Select(g => g.Group)
            .ToArray();

        CollectionAssert.IsSubsetOf(new[] { "SkiaSharp", "HarfBuzzSharp" }, groups, "The manifest no longer has the SkiaSharp and HarfBuzzSharp groups this test expects.");
        var offenders = groups.Where(g => !excluded.Contains(g)).ToArray();
        Assert.AreEqual(0, offenders.Length, $"Groups with SkiaSharp.* or HarfBuzzSharp.* packages missing from the exclude list: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    [TestCategory("Online")] // Reads the pinned packages' nuspecs on nuget.org, like the updater run in the same CI job
    public async Task Manifest_HarfBuzzSharp_IsFromTheSkiaSharpRelease()
    {
        // Both groups move by hand, so this is the check that a SkiaSharp move also moved HarfBuzzSharp, to the same release.
        using var client = new NuGetApiClient();

        var mismatch = await SkiaSharpRelease.FindHarfBuzzSharpMismatchAsync(LoadManifest(), client);

        Assert.IsNull(mismatch, mismatch);
    }

    private static ManifestGroup Group(string name, string package) => new(name, "10.0.120", [package]);

    private static IEnumerable<ManifestGroup> LoadManifest()
    {
        var path = FindRepoFile("src/Uno.Sdk/packages.json");
        return JsonSerializer.Deserialize<ManifestGroup[]>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Could not read {path}");
    }

    // The exclude list the updater workflow passes with --exclude-file (.github/actions/sdk-update/action.yml)
    private static HashSet<string> LoadExcludeList() => ExcludeConfig.Load(FindRepoFile(".github/sdk-update-exclude.json"));

    private static string FindRepoFile(string relativePath)
    {
        // The test runs from tools/Uno.Sdk.Updater.Tests/bin/...: walk up to the repository root.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            // Path.Join, not Path.Combine: it never drops the directory, even for a rooted relativePath
            var path = Path.Join(dir.FullName, relativePath);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"{relativePath} was not found above the test directory.");
    }
}
