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
}
