using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Models;
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
    public void MajorUpgradeDisabledGroups_KeepTheirMembers()
    {
        CollectionAssert.AreEquivalent(
            new[] { "SkiaSharp", "WasmBootstrap", "MicrosoftLoggingConsole", "Maui", "MauiCompatibility" },
            UpdatePolicy.MajorUpgradeDisabledGroups);
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
        var maui = manifest["Maui"];
        var mauiNet10 = maui.VersionOverride is not null && maui.VersionOverride.TryGetValue("net10.0", out var v) ? v : maui.Version;

        Assert.AreEqual(mauiNet10, manifest["MauiCompatibility"].Version,
            "MauiCompatibility must pin the same MAUI version as the Maui group's net10.0 version.");
    }

    private static ManifestGroup Group(string name, string package) => new(name, "10.0.120", [package]);

    private static IEnumerable<ManifestGroup> LoadManifest()
    {
        // The test runs from tools/Uno.Sdk.Updater.Tests/bin/...: walk up to the repository's Uno.Sdk manifest.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "src", "Uno.Sdk", "packages.json");
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<ManifestGroup[]>(File.ReadAllText(path))
                    ?? throw new InvalidOperationException($"Could not read {path}");
            }
        }

        throw new FileNotFoundException("src/Uno.Sdk/packages.json was not found above the test directory.");
    }
}
