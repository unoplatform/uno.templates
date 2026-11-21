using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Models;
using Uno.Sdk.Services;
using Uno.Sdk.Updater;

namespace Uno.Sdk.Updater.Tests;

[TestClass]
public class ReadMeTableTests
{
    private const string Template = """
        # Uno.Sdk

        Intro.

        <!-- VersionProperties:Start -->
        <!-- comment -->

        | MSBuild Property | Default Version |
        |----------------|:---------------:|
        | UnoVersion* | $Core$ |
        | PrismVersion | $Prism$ |

        <!-- VersionProperties:End -->

        \* footnote
        """;

    private static readonly ManifestGroup[] Manifest =
    [
        new("Core", "7.1.0-dev.500", ["Uno.WinUI"]),
        new("CSharpMarkup", "7.0.1", ["Uno.WinUI.Markup"], VersionProperty: "UnoCSharpMarkupVersion"),
        new("WasmBootstrap", "10.0.98", ["Uno.Wasm.Bootstrap"], new() { ["net11.0"] = "11.1.0-dev.5" }, "UnoWasmBootstrapVersion"),
        new("Maui", "10.0.120", ["Microsoft.Maui.Controls"], VersionProperty: "MauiVersion"),
        new("MauiCompatibility", "10.0.120", ["Microsoft.Maui.Controls.Compatibility"], VersionProperty: "MauiVersion"),
        new("NoProperty", "1.0.0", ["Some.Package"]),
    ];

    [TestMethod]
    public void Table_IsWrittenFromTheVersionProperties()
    {
        var readMe = ReadMeTable.ReplaceVersionPropertiesTable(Template, Manifest);

        StringAssert.Contains(readMe, "| UnoVersion* | 7.1.0-dev.500 |");
        StringAssert.Contains(readMe, "| UnoCSharpMarkupVersion | 7.0.1 |");
        StringAssert.Contains(readMe, "| UnoWasmBootstrapVersion** | 10.0.98 |", "A group with a versionOverride gets the ** footnote.");
        Assert.AreEqual(1, CountOf(readMe, "| MauiVersion |"), "A property that drives two groups is listed once.");
        Assert.IsFalse(readMe.Contains("PrismVersion"), "Rows of the template are replaced.");
        Assert.IsFalse(readMe.Contains("Some.Package"));
    }

    [TestMethod]
    public void TextOutsideTheMarkers_IsKept()
    {
        var readMe = ReadMeTable.ReplaceVersionPropertiesTable(Template, Manifest);

        StringAssert.StartsWith(readMe, "# Uno.Sdk");
        StringAssert.Contains(readMe, "Intro.");
        StringAssert.Contains(readMe, ReadMeTable.StartMarker);
        StringAssert.Contains(readMe, ReadMeTable.EndMarker);
        StringAssert.Contains(readMe, "\\* footnote");
    }

    [TestMethod]
    public void Table_IsSurroundedByBlankLines()
    {
        var readMe = ReadMeTable.ReplaceVersionPropertiesTable(Template, Manifest).Replace("\r\n", "\n");

        StringAssert.Contains(readMe, ReadMeTable.StartMarker + "\n\n| MSBuild Property");
        StringAssert.Contains(readMe, " |\n\n" + ReadMeTable.EndMarker);
    }

    [TestMethod]
    public void OlderSdkWithoutTheField_KeepsTheTemplateTable()
    {
        var manifest = new[] { new ManifestGroup("Core", "7.0.29", ["Uno.WinUI"]) };

        Assert.AreEqual(Template, ReadMeTable.ReplaceVersionPropertiesTable(Template, manifest));
    }

    [TestMethod]
    public void TemplateWithoutMarkers_IsUnchanged()
    {
        const string readMe = "# Uno.Sdk\n\n| MSBuild Property | Default Version |\n";

        Assert.AreEqual(readMe, ReadMeTable.ReplaceVersionPropertiesTable(readMe, Manifest));
    }

    [TestMethod]
    public void RealTemplate_HasTheMarkers()
    {
        var template = File.ReadAllText(Path.Join(LocalFileSystem.UnoSdkUpdaterDirectory, SdkFiles.ReadMeFileName));

        var readMe = ReadMeTable.ReplaceVersionPropertiesTable(template, Manifest);

        StringAssert.Contains(readMe, "| UnoCSharpMarkupVersion | 7.0.1 |");
        Assert.IsFalse(readMe.Contains("$Prism$"));
    }

    private static int CountOf(string text, string value)
        => (text.Length - text.Replace(value, string.Empty).Length) / value.Length;
}
