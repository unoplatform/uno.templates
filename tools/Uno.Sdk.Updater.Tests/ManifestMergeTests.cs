using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Models;
using Uno.Sdk.Services;

namespace Uno.Sdk.Updater.Tests;

[TestClass]
public class ManifestMergeTests
{
    // Same options Program.cs uses to write packages.json.
    private static readonly JsonSerializerOptions WriteOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string SdkManifestJson = """
        [
          { "group": "Core", "version": "7.1.0-dev.500", "packages": [ "Uno.WinUI" ] },
          { "group": "CSharpMarkup", "version": "7.0.1", "packages": [ "Uno.WinUI.Markup", "Uno.Extensions.Markup.Generators" ], "versionProperty": "UnoCSharpMarkupVersion" },
          { "group": "UnoFonts", "version": "2.10.1", "packages": [ "Uno.Fonts.OpenSans" ], "versionProperty": "UnoFontsVersion" }
        ]
        """;

    // The local copy predates the field and has no UnoFonts group.
    private const string LocalManifestJson = """
        [
          { "group": "Core", "version": "7.1.0-dev.400", "packages": [ "Uno.WinUI" ] },
          { "group": "CSharpMarkup", "version": "7.0.3", "packages": [ "Uno.WinUI.Markup" ], "versionOverride": { "net11.0": "7.1.0-dev.5" } }
        ]
        """;

    [TestMethod]
    [DataRow(true, DisplayName = "Preview Uno")]
    [DataRow(false, DisplayName = "Release Uno")]
    public void VersionProperty_IsTakenFromTheSdk(bool isPreviewUno)
    {
        var merged = Merge(isPreviewUno);

        Assert.AreEqual("UnoCSharpMarkupVersion", Group(merged, "CSharpMarkup").VersionProperty);
        Assert.AreEqual("UnoFontsVersion", Group(merged, "UnoFonts").VersionProperty);
        Assert.IsNull(Group(merged, "Core").VersionProperty);
    }

    [TestMethod]
    [DataRow(true, DisplayName = "Preview Uno")]
    [DataRow(false, DisplayName = "Release Uno")]
    public void LocalVersions_AreKept(bool isPreviewUno)
    {
        var markup = Group(Merge(isPreviewUno), "CSharpMarkup");

        Assert.AreEqual("7.0.3", markup.Version);
        Assert.AreEqual("7.1.0-dev.5", markup.VersionOverride!["net11.0"]);
    }

    [TestMethod]
    [DataRow(true, DisplayName = "Preview Uno")]
    [DataRow(false, DisplayName = "Release Uno")]
    public void OlderSdkWithoutTheField_KeepsTheLocalValue(bool isPreviewUno)
    {
        var sdk = Parse("""[ { "group": "CSharpMarkup", "version": "7.0.1", "packages": [ "Uno.WinUI.Markup" ] } ]""");
        var local = Parse("""[ { "group": "CSharpMarkup", "version": "7.0.1", "packages": [ "Uno.WinUI.Markup" ], "versionProperty": "UnoCSharpMarkupVersion" } ]""");

        var merged = isPreviewUno
            ? ManifestMerge.MergeLocalOverridesOnly(sdk, local)
            : ManifestMerge.MergeLocalManifest(sdk, local);

        Assert.AreEqual("UnoCSharpMarkupVersion", Group(merged, "CSharpMarkup").VersionProperty);
    }

    [TestMethod]
    public void WrittenManifest_PutsTheFieldLast_AndNeverWritesNull()
    {
        var json = JsonSerializer.Serialize(Merge(isPreviewUno: true), WriteOptions);

        using var document = JsonDocument.Parse(json);
        foreach (var group in document.RootElement.EnumerateArray())
        {
            var names = group.EnumerateObject().Select(p => p.Name).ToList();
            var name = group.GetProperty("group").GetString();

            if (name == "Core")
            {
                CollectionAssert.DoesNotContain(names, "versionProperty", "A group without a version property must not get one written.");
            }
            else
            {
                Assert.AreEqual("versionProperty", names[^1], $"Group {name}: versionProperty must be the last key.");
            }
        }
    }

    [TestMethod]
    public void WrittenManifest_RoundTrips()
    {
        var merged = Merge(isPreviewUno: true).ToList();

        var roundTripped = Parse(JsonSerializer.Serialize(merged, WriteOptions)).ToList();

        Assert.AreEqual(merged.Count, roundTripped.Count);
        foreach (var group in merged)
        {
            Assert.AreEqual(group.VersionProperty, Group(roundTripped, group.Group).VersionProperty, group.Group);
        }
    }

    private static IEnumerable<ManifestGroup> Merge(bool isPreviewUno)
        => isPreviewUno
            ? ManifestMerge.MergeLocalOverridesOnly(Parse(SdkManifestJson), Parse(LocalManifestJson))
            : ManifestMerge.MergeLocalManifest(Parse(SdkManifestJson), Parse(LocalManifestJson));

    // Program.cs reads packages.json with the default options.
    private static IEnumerable<ManifestGroup> Parse(string json)
        => JsonSerializer.Deserialize<IEnumerable<ManifestGroup>>(json) ?? [];

    private static ManifestGroup Group(IEnumerable<ManifestGroup> manifest, string name)
        => manifest.Single(g => string.Equals(g.Group, name, StringComparison.OrdinalIgnoreCase));
}
