using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Services;
using Uno.Sdk.Updater;

namespace Uno.Sdk.Updater.Tests;

[TestClass]
public class SdkFilesTests
{
    private const string ManifestJson = """
        [
          { "group": "Core", "version": "7.1.0-dev.500", "packages": [ "Uno.WinUI" ] },
          { "group": "WasmBootstrap", "version": "10.0.98", "packages": [ "Uno.Wasm.Bootstrap" ], "versionOverride": { "net10.0": "10.1.1", "net11.0": "11.1.0-dev.5" }, "versionProperty": "UnoWasmBootstrapVersion" },
          { "group": "CSharpMarkup", "version": "7.0.1", "packages": [ "Uno.WinUI.Markup" ], "versionProperty": "UnoCSharpMarkupVersion" }
        ]
        """;

    private const string Template = """
        # Uno.Sdk

        <!-- VersionProperties:Start -->

        | MSBuild Property | Default Version |
        |----------------|:---------------:|
        | UnoVersion* | $Core$ |

        <!-- VersionProperties:End -->

        Markup: $CSharpMarkup$

        ```json
        $PackagesJson$
        ```
        """;

    private string _root = null!;

    [TestInitialize]
    public void Setup()
    {
        _root = Path.Join(Path.GetTempPath(), "sdkfiles-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Join(_root, "src", "Uno.Sdk"));
        Directory.CreateDirectory(Path.Join(_root, "tools"));

        File.WriteAllText(Path.Join(UnoSdk, "packages.json"), ManifestJson);
        File.WriteAllText(TemplatePath, Template);
        File.WriteAllText(Path.Join(UnoSdk, "Uno.Sdk.Updater.props"), SdkFiles.BuildSdkProps("uno platform", "7.1.0-dev.500"));
        SdkFiles.Regenerate(UnoSdk, Src, TemplatePath);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    private string Src => Path.Join(_root, "src");

    private string UnoSdk => Path.Join(_root, "src", "Uno.Sdk");

    private string TemplatePath => Path.Join(_root, "tools", "ReadMe.md");

    [TestMethod]
    public void RegeneratedFiles_AreInSync()
    {
        CollectionAssert.AreEqual(Array.Empty<string>(), SdkFiles.FindDrift(UnoSdk, Src, TemplatePath).ToArray());
    }

    [TestMethod]
    public void ReadMe_HasTheTableVersionsAndEmbeddedManifest()
    {
        var readMe = File.ReadAllText(Path.Join(UnoSdk, "ReadMe.md"));

        StringAssert.Contains(readMe, "| UnoCSharpMarkupVersion | 7.0.1 |");
        StringAssert.Contains(readMe, "Markup: 7.0.1");
        StringAssert.Contains(readMe, "\"versionProperty\": \"UnoCSharpMarkupVersion\"");
    }

    [TestMethod]
    public void Targets_TakeTheWasmBootstrapOverrides()
    {
        var targets = File.ReadAllText(Path.Join(Src, "Uno.Sdk.Updater.targets"));

        StringAssert.Contains(targets, "<UnoWasmBootstrapVersionNet10>10.1.1</UnoWasmBootstrapVersionNet10>");
        StringAssert.Contains(targets, "<UnoWasmBootstrapVersionNet11>11.1.0-dev.5</UnoWasmBootstrapVersionNet11>");
    }

    [TestMethod]
    public void Props_KeepTheirTags()
    {
        StringAssert.Contains(File.ReadAllText(Path.Join(UnoSdk, "Uno.Sdk.Updater.props")), "<PackageTags>uno platform</PackageTags>");
    }

    [TestMethod]
    [DataRow("ReadMe.md", "| UnoCSharpMarkupVersion | 7.0.1 |", "| UnoCSharpMarkupVersion | 9.9.9 |", DisplayName = "ReadMe table edited")]
    [DataRow("ReadMe.md", "\"version\": \"7.0.1\"", "\"version\": \"9.9.9\"", DisplayName = "ReadMe embedded manifest edited")]
    [DataRow("Uno.Sdk.Updater.props", "<SdkVersion>7.1.0-dev.500</SdkVersion>", "<SdkVersion>7.0.29</SdkVersion>", DisplayName = "Props SdkVersion not the Core version")]
    public void HandEdit_IsReported(string file, string from, string to)
    {
        var path = Path.Join(UnoSdk, file);
        File.WriteAllText(path, File.ReadAllText(path).Replace(from, to));

        var drift = SdkFiles.FindDrift(UnoSdk, Src, TemplatePath);

        Assert.AreEqual(1, drift.Count, string.Join(Environment.NewLine, drift));
        StringAssert.Contains(drift[0], file);
    }

    [TestMethod]
    public void TargetsEdit_IsReported()
    {
        var path = Path.Join(Src, "Uno.Sdk.Updater.targets");
        File.WriteAllText(path, File.ReadAllText(path).Replace("10.1.1", "9.0.0"));

        var drift = SdkFiles.FindDrift(UnoSdk, Src, TemplatePath);

        Assert.AreEqual(1, drift.Count);
        StringAssert.Contains(drift[0], "Uno.Sdk.Updater.targets");
    }

    [TestMethod]
    public void ManifestChangedWithoutRerunningTheUpdater_IsReported()
    {
        var path = Path.Join(UnoSdk, "packages.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"7.0.1\"", "\"7.0.3\""));

        var drift = SdkFiles.FindDrift(UnoSdk, Src, TemplatePath);

        Assert.IsTrue(drift.Any(d => d.Contains("ReadMe.md")), string.Join(Environment.NewLine, drift));
    }

    [TestMethod]
    public void MissingFile_IsReported()
    {
        File.Delete(Path.Join(Src, "Uno.Sdk.Updater.targets"));

        StringAssert.Contains(SdkFiles.FindDrift(UnoSdk, Src, TemplatePath).Single(), "is missing");
    }

    [TestMethod]
    public void LineEndingsAndByteOrderMark_AreNotDrift()
    {
        var path = Path.Join(UnoSdk, "ReadMe.md");
        File.WriteAllText(path, "﻿" + File.ReadAllText(path).Replace("\r\n", "\n").Replace("\n", "\r\n"));

        CollectionAssert.AreEqual(Array.Empty<string>(), SdkFiles.FindDrift(UnoSdk, Src, TemplatePath).ToArray());
    }

    /// <summary>
    /// The files committed in this repository: same check as the CI step.
    /// </summary>
    [TestMethod]
    public void CommittedFiles_AreInSync()
    {
        var drift = SdkFiles.FindDrift(
            LocalFileSystem.UnoSdkDirectory,
            LocalFileSystem.TemplatesSourceDirectory,
            Path.Join(LocalFileSystem.UnoSdkUpdaterDirectory, SdkFiles.ReadMeFileName));

        Assert.AreEqual(0, drift.Count, "Run: dotnet run --project tools/Uno.Sdk.Updater -- --regenerate" + Environment.NewLine + string.Join(Environment.NewLine, drift));
    }
}
