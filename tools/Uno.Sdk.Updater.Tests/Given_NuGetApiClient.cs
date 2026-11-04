using Uno.Sdk.Models;
using Uno.Sdk.Services;
using Uno.Sdk.Updater.Utils;

namespace Uno.Sdk.Updater.Tests;

// The client's UnoVersion stays unset, so the Uno packages skip the nuspec validation (it reads nuget.org).
public class Given_NuGetApiClient
{
    [TestCase("Microsoft.Extensions.Logging.Console", "10.0.13", new[] { "9.0.21", "10.0.13", "10.0.14", "11.0.0" }, "10.0.14")]
    [TestCase("Microsoft.Maui.Controls", "10.0.110", new[] { "10.0.110", "10.0.120", "11.0.0" }, "10.0.120")]
    [TestCase("Uno.Wasm.Bootstrap", "10.1.0-dev.219", new[] { "10.1.0-dev.219", "10.1.0-dev.284", "11.1.0-dev.7" }, "10.1.0-dev.284")]
    public async Task When_Override_Then_Stays_On_Its_DotNet_Major(string packageId, string pinned, string[] feed, string expected)
        => Assert.That(await GetOverrideVersion(packageId, "net10.0", pinned, feed), Is.EqualTo(expected));

    [Test]
    public async Task When_Override_Above_Its_DotNet_Major_Then_Still_Updates()
        => Assert.That(await GetOverrideVersion("Contoso.Widgets", "net10.0", "12.0.0", ["12.0.0", "12.1.0"]), Is.EqualTo("12.1.0"));

    [TestCase("Uno.Extensions.Core.WinUI", "7.4.0-dev.85", new[] { "7.4.0-dev.85", "7.4.0-dev.103", "8.0.0-dev.5" }, true, "7.4.0-dev.103")]
    [TestCase("Uno.Extensions.Core.WinUI", "7.4.0-dev.85", new[] { "7.4.0-dev.85", "7.4.0-dev.103", "8.0.0-dev.5" }, false, "8.0.0-dev.5")]
    [TestCase("Uno.Resizetizer", "1.13.0-dev.21", new[] { "1.13.0-dev.21", "1.13.0-dev.24", "2.0.0-dev.45" }, true, "1.13.0-dev.24")]
    public async Task When_Major_Upgrade_Disabled_Then_Stays_On_Pinned_Major(string packageId, string pinned, string[] feed, bool majorUpgradeDisabled, string expected)
    {
        using var client = new NuGetApiClient(_ => feed);

        Assert.That(await client.GetVersionAsync(packageId, true, majorUpgradeDisabled, pinned), Is.EqualTo(expected));
    }

    // The same call the updater makes for each versionOverride entry (UpdateGroup in Program.cs).
    private static async Task<string> GetOverrideVersion(string packageId, string key, string pinned, string[] feed)
    {
        using var client = new NuGetApiClient(_ => feed);
        NuGetVersion pinnedVersion = pinned;

        return await client.GetVersionAsync(packageId, pinnedVersion.IsPreview, false, pinned, TargetFrameworkOverride.GetMaxMajor(key, pinnedVersion));
    }
}
