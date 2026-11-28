#nullable enable
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Uno.Sdk.Models;

namespace Uno.Sdk.Services;

/// <summary>
/// Builds the files the updater derives from src/Uno.Sdk/packages.json, and checks that the
/// committed copies still match it.
/// </summary>
/// <remarks>
/// The updater writes four files together: packages.json, the Uno.Sdk ReadMe (property table,
/// versions and an embedded copy of packages.json), Uno.Sdk.Updater.props (the Uno.Sdk.Private
/// version) and Uno.Sdk.Updater.targets (the versions the templates use). <see cref="FindDrift"/>
/// rebuilds the last three from packages.json with the same code the updater uses and reports any
/// difference, so a hand edit or a partial update fails CI instead of shipping.
/// </remarks>
internal static class SdkFiles
{
    public const string SdkPackageId = "Uno.Sdk.Private";
    public const string PackagesJsonFileName = "packages.json";
    public const string ReadMeFileName = "ReadMe.md";
    public const string UpdaterPropsFileName = "Uno.Sdk.Updater.props";
    public const string UpdaterTargetsFileName = "Uno.Sdk.Updater.targets";

    /// <summary>Where packages.json goes inside the Uno.Sdk package.</summary>
    public const string PackagesJsonPackagePath = "targets/netstandard2.0/packages.json";

    /// <summary>
    /// The ReadMe: version property table, $Group$ placeholders and the embedded packages.json.
    /// </summary>
    public static string BuildReadMe(string template, string manifestJson)
    {
        var manifest = ParseManifest(manifestJson);

        var readMe = ReadMeTable.ReplaceVersionPropertiesTable(template, manifest);

        foreach (var group in manifest)
        {
            readMe = Regex.Replace(readMe, Regex.Escape($"${group.Group}$"), group.Version);
        }

        return Regex.Replace(readMe, Regex.Escape("$PackagesJson$"), manifestJson);
    }

    /// <summary>
    /// Uno.Sdk.Updater.targets: only the versions that Uno.Templates.csproj writes into the templates.
    /// </summary>
    public static string BuildUpdaterTargets(IEnumerable<ManifestGroup> manifest)
    {
        var props = new Dictionary<string, string>
        {
            { "UnoWasmBootstrapVersionNet10", GetManifestGroupVersionOverride(manifest, "WasmBootstrap", "net10.0") },
            { "UnoWasmBootstrapVersionNet11", GetManifestGroupVersionOverride(manifest, "WasmBootstrap", "net11.0") },
        };

        return CreateMSBuildFile(props);
    }

    /// <summary>
    /// Uno.Sdk.Updater.props: the Uno.Sdk.Private version and the files packed in the Uno.Sdk.
    /// </summary>
    public static string BuildSdkProps(string? tags, string sdkVersion)
    {
        var props = new Dictionary<string, string>
        {
            { "PackageTags", tags ?? string.Empty },
            { "SdkPackageId", SdkPackageId },
            { "SdkVersion", sdkVersion },
            { "PackageReadmeFile", ReadMeFileName }
        };

        var readMe = new MsBuildItem(ReadMeFileName, new Dictionary<string, string>
        {
            { "Pack", bool.TrueString },
            { "PackagePath", ReadMeFileName }
        });

        var packagesJson = new MsBuildItem(PackagesJsonFileName, new Dictionary<string, string>
        {
            { "Pack", bool.TrueString },
            { "PackagePath", PackagesJsonPackagePath }
        });

        return CreateMSBuildFile(props, readMe, packagesJson);
    }

    /// <summary>
    /// Compares the committed ReadMe, props and targets with what packages.json produces.
    /// Returns one message per file that differs; empty when everything is in sync.
    /// </summary>
    /// <param name="unoSdkDirectory">src/Uno.Sdk (packages.json, ReadMe.md, Uno.Sdk.Updater.props).</param>
    /// <param name="templatesSourceDirectory">src (Uno.Sdk.Updater.targets).</param>
    /// <param name="readMeTemplatePath">tools/Uno.Sdk.Updater/ReadMe.md.</param>
    public static IReadOnlyList<string> FindDrift(string unoSdkDirectory, string templatesSourceDirectory, string readMeTemplatePath)
    {
        var drift = new List<string>();

        foreach (var (path, expected, what) in BuildExpectedFiles(unoSdkDirectory, templatesSourceDirectory, readMeTemplatePath))
        {
            if (!File.Exists(path))
            {
                drift.Add($"{path} is missing ({what}).");
            }
            else if (Normalize(File.ReadAllText(path)) != Normalize(expected))
            {
                drift.Add($"{path} doesn't match packages.json ({what}).");
            }
        }

        return drift;
    }

    /// <summary>
    /// Rewrites the ReadMe, props and targets from packages.json (no download). Returns the files written.
    /// </summary>
    public static IReadOnlyList<string> Regenerate(string unoSdkDirectory, string templatesSourceDirectory, string readMeTemplatePath)
    {
        var written = new List<string>();

        foreach (var (path, expected, _) in BuildExpectedFiles(unoSdkDirectory, templatesSourceDirectory, readMeTemplatePath))
        {
            if (!File.Exists(path) || Normalize(File.ReadAllText(path)) != Normalize(expected))
            {
                File.WriteAllText(path, expected, Encoding.UTF8);
                written.Add(path);
            }
        }

        return written;
    }

    public static string GetManifestGroupVersionOverride(IEnumerable<ManifestGroup> manifest, string groupId, string overrideKey)
    {
        var group = manifest.First(x => x.Group == groupId);
        if (group.VersionOverride is not null && group.VersionOverride.Count != 0)
        {
            return group.VersionOverride[overrideKey];
        }

        throw new InvalidOperationException($"No Version Overrides were found for {groupId} or the key {overrideKey}.");
    }

    public static string CreateMSBuildFile(IDictionary<string, string> props, params MsBuildItem[] items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<Project>");
        builder.AppendLine("  <!-- This file is generated by the Uno.Sdk.Updater. Do not make manual changes. -->");
        builder.AppendLine("  <PropertyGroup>");

        foreach ((var key, var value) in props)
        {
            builder.AppendLine($"    <{key}>{value}</{key}>");
        }

        builder.AppendLine("  </PropertyGroup>");

        if (items.Length != 0)
        {
            builder.AppendLine();
            builder.AppendLine("  <ItemGroup>");
            foreach (var item in items)
            {
                builder.AppendLine($"    {item.ToXml()}");
            }
            builder.AppendLine("  </ItemGroup>");
        }
        builder.AppendLine("</Project>");
        return builder.ToString();
    }

    private static IEnumerable<(string Path, string Expected, string What)> BuildExpectedFiles(string unoSdkDirectory, string templatesSourceDirectory, string readMeTemplatePath)
    {
        var manifestJson = File.ReadAllText(Path.Join(unoSdkDirectory, PackagesJsonFileName));
        var manifest = ParseManifest(manifestJson);

        yield return (
            Path.Join(unoSdkDirectory, ReadMeFileName),
            BuildReadMe(File.ReadAllText(readMeTemplatePath), manifestJson),
            "property table, versions and embedded packages.json");

        // PackageTags come from the Uno.Sdk.Private nuspec, which isn't available offline: keep the committed value.
        var propsPath = Path.Join(unoSdkDirectory, UpdaterPropsFileName);
        var tags = File.Exists(propsPath)
            ? Regex.Match(File.ReadAllText(propsPath), "<PackageTags>(.*?)</PackageTags>").Groups[1].Value
            : string.Empty;
        var core = manifest.FirstOrDefault(g => g.Group == "Core")
            ?? throw new InvalidOperationException("packages.json has no Core group.");

        yield return (propsPath, BuildSdkProps(tags, core.Version), "SdkVersion must be the Core version");

        yield return (
            Path.Join(templatesSourceDirectory, UpdaterTargetsFileName),
            BuildUpdaterTargets(manifest),
            "WasmBootstrap versions");
    }

    private static List<ManifestGroup> ParseManifest(string manifestJson)
        => JsonSerializer.Deserialize<List<ManifestGroup>>(manifestJson.TrimStart('﻿'))
            ?? throw new InvalidOperationException("Unable to parse packages.json.");

    // Line endings and a byte-order mark depend on the platform and git settings, not on the content.
    private static string Normalize(string text)
        => text.TrimStart('﻿').Replace("\r\n", "\n");
}
