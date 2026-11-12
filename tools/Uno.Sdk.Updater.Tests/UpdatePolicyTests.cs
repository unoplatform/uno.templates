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
    public void OtherGroups_AreNotHeld_OnReleaseBranches()
    {
        Assert.IsFalse(UpdatePolicy.IsHeldToAvoidJavaMisalignment(Group("Toolkit", "Uno.Toolkit.WinUI"), isPreviewUno: false));
    }

    [TestMethod]
    public void MauiGroups_FollowTheSameRules()
    {
        // The two groups are one MAUI version: a rule for one must apply to the other.
        CollectionAssert.IsSubsetOf(UpdatePolicy.MauiGroups, UpdatePolicy.MajorUpgradeDisabledGroups);
    }

    private static ManifestGroup Group(string name, string package) => new(name, "10.0.120", [package]);
}
