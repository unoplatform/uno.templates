using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Models;

namespace Uno.Sdk.Updater.Tests;

[TestClass]
public class NuGetVersionTests
{
    [TestMethod]
    [DataRow("7.0.0-dev.2333", "7.0.0-dev.2333", DisplayName = "Plain version")]
    [DataRow("[0.1.0-dev.1]", "0.1.0-dev.1", DisplayName = "Exact version")]
    [DataRow("[1.2.3,2.0.0)", "1.2.3", DisplayName = "Bounded range")]
    [DataRow("(1.2.3,)", "1.2.3", DisplayName = "Exclusive lower bound")]
    [DataRow("[1.2.3+sha]", "1.2.3", DisplayName = "Build metadata")]
    public void TryParseMinimumVersion_ReturnsLowerBound(string range, string expected)
    {
        Assert.IsTrue(NuGetVersion.TryParseMinimumVersion(range, out var version));
        Assert.AreEqual(expected, version.OriginalVersion);
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Missing")]
    [DataRow("", DisplayName = "Empty")]
    [DataRow("(,2.0.0]", DisplayName = "No lower bound")]
    [DataRow("not-a-version", DisplayName = "Malformed")]
    public void TryParseMinimumVersion_WithoutLowerBound_ReturnsFalse(string? range) =>
        Assert.IsFalse(NuGetVersion.TryParseMinimumVersion(range, out _));

    [TestMethod]
    [DataRow("11.0.0-rc.3.26522.148", "11.0.0", true, DisplayName = "Release candidate before its release")]
    [DataRow("11.0.0", "11.0.0-rc.3.26522.148", false, DisplayName = "Release after its release candidate")]
    [DataRow("11.0.0-rc.2.26505.5", "11.0.0-rc.3.26522.148", true, DisplayName = "Older release candidate")]
    [DataRow("10.0.130", "11.0.0-rc.2.26505.5", true, DisplayName = "Lower version")]
    [DataRow("11.0.10", "11.0.0", false, DisplayName = "Higher version")]
    [DataRow("11.0.0", "11.0.0", false, DisplayName = "Same version")]
    public void IsOlderThan_FollowsReleaseOrder(string version, string other, bool expected) =>
        Assert.AreEqual(expected, NuGetVersion.Parse(version).IsOlderThan(NuGetVersion.Parse(other)));

    // The version windows rely on this order: a preview of the next version stays above a stable cap.
    [TestMethod]
    public void CompareTo_RanksPreviewAfterItsRelease() =>
        Assert.IsTrue(NuGetVersion.Parse("7.1.0-dev.5") > NuGetVersion.Parse("7.1.0"));
}
