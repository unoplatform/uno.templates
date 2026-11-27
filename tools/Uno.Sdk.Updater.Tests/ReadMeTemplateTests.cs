using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Sdk.Updater.Tests;

// The updater fills tools/Uno.Sdk.Updater/ReadMe.md into src/Uno.Sdk/ReadMe.md, the nuget.org ReadMe of the
// Uno.Sdk: each $<group>$ becomes that manifest group's version, matched case-sensitively (Program.cs).
[TestClass]
public class ReadMeTemplateTests
{
    private const string TemplatePath = "tools/Uno.Sdk.Updater/ReadMe.md";
    private const string ShippedReadMePath = "src/Uno.Sdk/ReadMe.md";

    // Filled with the manifest itself, not a group version
    private const string PackagesJsonPlaceholder = "PackagesJson";

    // Groups without a row of their own: the property of another group moves them too.
    private static readonly string[] GroupsWithoutARow =
    [
        // MauiVersion moves it with the Maui group (ImplicitPackagesResolver in unoplatform/uno)
        "MauiCompatibility",
    ];

    [TestMethod]
    public void Template_EveryPlaceholder_IsAManifestGroup()
    {
        // A placeholder without a group ships as is, such as "$Prism$" once the Prism group left the manifest.
        var groups = RepoFiles.LoadManifest().Select(g => g.Group).ToHashSet(StringComparer.Ordinal);

        var offenders = GetPlaceholders(ReadRepoFile(TemplatePath))
            .Where(p => p != PackagesJsonPlaceholder && !groups.Contains(p))
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
            $"{TemplatePath} has placeholders that match no group of src/Uno.Sdk/packages.json (names are case-sensitive): {string.Join(", ", offenders)}. Remove their rows, or fix the group name.");
    }

    [TestMethod]
    public void Template_EveryManifestGroup_HasARow()
    {
        // A group the updater adds from the Uno.Sdk.Private manifest needs a row for its version override property.
        var placeholders = GetPlaceholders(ReadRepoFile(TemplatePath)).ToHashSet(StringComparer.Ordinal);

        var offenders = RepoFiles.LoadManifest()
            .Select(g => g.Group)
            .Where(g => !placeholders.Contains(g) && !GroupsWithoutARow.Contains(g))
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
            $"Manifest groups without a row in {TemplatePath}: {string.Join(", ", offenders)}. Add a '| <property> | $<group>$ |' row with the MSBuild property the Uno.Sdk reads for it (ImplicitPackagesResolver in unoplatform/uno), then regenerate {ShippedReadMePath}.");
    }

    [TestMethod]
    public void ShippedReadMe_ListsTheTemplateProperties()
    {
        // The updater only rewrites the shipped ReadMe when it changes a version, so a template edit
        // must regenerate it in the same change.
        var expected = GetTableProperties(ReadRepoFile(TemplatePath));
        var actual = GetTableProperties(ReadRepoFile(ShippedReadMePath));

        CollectionAssert.AreEqual(expected, actual,
            $"The properties in {ShippedReadMePath} differ from {TemplatePath}. Expected: {string.Join(", ", expected)}. Actual: {string.Join(", ", actual)}.");
    }

    private static string ReadRepoFile(string relativePath) => File.ReadAllText(RepoFiles.Find(relativePath));

    private static IEnumerable<string> GetPlaceholders(string content) =>
        Regex.Matches(content, @"\$(?<name>[A-Za-z0-9]+)\$").Select(m => m.Groups["name"].Value).Distinct();

    // The first column of the property table. The header ("MSBuild Property") and separator rows don't match.
    private static string[] GetTableProperties(string content) =>
        content.Split('\n')
            .Select(line => Regex.Match(line.TrimEnd('\r'), @"^\|\s*(?<property>[A-Za-z0-9]+\**)\s*\|"))
            .Where(m => m.Success)
            .Select(m => m.Groups["property"].Value)
            .ToArray();
}
