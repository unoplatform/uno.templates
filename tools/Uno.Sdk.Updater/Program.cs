using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Uno.Sdk.Models;
using Uno.Sdk.Services;
using Uno.Sdk.Updater;

const string UnoSdkPackageId = "Uno.Sdk";

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    // We want to keep the output Human Readable
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

using var client = new NuGetApiClient();

var versions = await client.GetPackageVersions(UnoSdkPackageId);

versions = versions.Where(x => x > LocalFileSystem.TemplateVersion);
var unoVersion = versions.OrderByDescending(x => x).FirstOrDefault();

using var sdkPackage = await client.DownloadPackageAsync(UnoSdkPackageId, unoVersion);
using var sdkZip = new ZipArchive(sdkPackage);

string? readMePath = null;
string? packagesJsonPath = null;
string? relativePackagesJsonPath = null;
string? description = null;
string? tags = null;

foreach(var entry in sdkZip.Entries)
{
    var extension = Path.GetExtension(entry.FullName);
    string[] allowedExtensions = [".md", ".json", ".nuspec"];
    if (!allowedExtensions.Any(x => x.Equals(extension, StringComparison.InvariantCultureIgnoreCase)) ||
        (extension.Equals(".json", StringComparison.InvariantCultureIgnoreCase) && !Path.GetFileName(entry.FullName).Equals("packages.json", StringComparison.InvariantCultureIgnoreCase)))
    {
        continue;
    }

    var outputPath = Path.Combine(LocalFileSystem.UnoSdkDirectory, entry.FullName);
    if (Path.GetFileName(outputPath).Equals("readme.md", StringComparison.InvariantCultureIgnoreCase))
    {
        readMePath = outputPath;
    }

    var directory = Path.GetDirectoryName(outputPath);
    if (string.IsNullOrEmpty(directory))
    {
        continue;
    }

    Directory.CreateDirectory(directory);
    if (entry.Name == "packages.json")
    {
        outputPath = Path.Combine(LocalFileSystem.UnoSdkDirectory, entry.Name);
        relativePackagesJsonPath = entry.FullName;
        packagesJsonPath = outputPath;
        using var packageStream = entry.Open();
        var inputManifest = await JsonSerializer.DeserializeAsync<IEnumerable<ManifestGroup>>(packageStream);

        var manifest = new List<ManifestGroup>();
        foreach(var group in inputManifest!)
        {
            var updated = await UpdateGroup(group, unoVersion, client);
            manifest.Add(updated);
        }

        CreateUpdaterTargets(manifest);
        var json = JsonSerializer.Serialize(manifest, jsonOptions);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
    }
    else if (extension == ".nuspec")
    {
        Console.WriteLine("Extracting NuSpec Metadata");
        var nuspec = NuGet.Packaging.Manifest.ReadFrom(entry.Open(), false);
        description = nuspec.Metadata.Description;
        tags = nuspec.Metadata.Tags;
    }
    else
    {
        entry.ExtractToFile(outputPath, true);
    }
}

if (string.IsNullOrEmpty(readMePath) || !File.Exists(readMePath))
{
    Console.WriteLine($"The downloaded {UnoSdkPackageId} did not contain a ReadMe.md, using local template.");
    readMePath = Path.Combine(LocalFileSystem.UnoSdkDirectory, "ReadMe.md");
    File.Copy("ReadMe.md", readMePath, true);
}

if (!string.IsNullOrEmpty(readMePath) && File.Exists(readMePath) &&
    !string.IsNullOrEmpty(packagesJsonPath) && File.Exists(packagesJsonPath))
{
    var readMe = File.ReadAllText(readMePath);
    var manifestJson = File.ReadAllText(packagesJsonPath);
    var manifest = JsonSerializer.Deserialize<IEnumerable<ManifestGroup>>(manifestJson) ?? [];

    foreach(var group in manifest)
    {
        readMe = Regex.Replace(readMe, Regex.Escape($"${group.Group}$"), group.Version);
    }

    readMe = Regex.Replace(readMe, Regex.Escape("$PackagesJson$"), manifestJson);

    Console.WriteLine("Updated the ReadMe with the versions used by this pack of the Uno.Sdk.");
    File.WriteAllText(readMePath, readMe, Encoding.UTF8);

    CreateSdkProps(description, tags, unoVersion, readMePath, packagesJsonPath, relativePackagesJsonPath ?? "ERROR - Unable to determine path");
}

Console.WriteLine("Finished updated.");

static void CreateSdkProps(string? description, string? tags, string unoVersion, string readMePath, string packagesJsonPath, string relativePackagesJsonPath)
{
    var props = new Dictionary<string, string>
    {
        { "Description", description ?? string.Empty },
        { "PackageTags", tags ?? string.Empty },
        { "SdkVersion", unoVersion },
        { "PackageReadmeFile", Path.GetFileName(readMePath) }
    };

    var readMe = new MsBuildItem(Path.GetRelativePath(LocalFileSystem.UnoSdkDirectory, readMePath), new Dictionary<string, string>
    {
        { "Pack", bool.TrueString },
        { "PackagePath", Path.GetFileName(readMePath) }
    });

    var packagesJson = new MsBuildItem(Path.GetRelativePath(LocalFileSystem.UnoSdkDirectory, packagesJsonPath), new Dictionary<string, string>
    {
        { "Pack", bool.TrueString },
        { "PackagePath", relativePackagesJsonPath }
    });

    Console.WriteLine("Creating Sdk Updater Props for the Uno.Sdk");
    var nuspecTargets = CreateMSBuildFile(props, readMe, packagesJson);
    File.WriteAllText(Path.Combine(LocalFileSystem.UnoSdkDirectory, "Uno.Sdk.Updater.props"), nuspecTargets);
}

static void CreateUpdaterTargets(IEnumerable<ManifestGroup> manifest)
{
    var props = new Dictionary<string, string>
    {
        { "UnoVersion", GetManifestGroupVersion(manifest, "Core") },
        { "UnoWasmBootstrapVersionNet8", GetManifestGroupVersion(manifest, "WasmBootstrap") },
        { "UnoWasmBootstrapVersionNet9", GetManifestGroupVersionOverride(manifest, "WasmBootstrap", "net9.0") },
        { "UnoExtensionsLoggingVersion", GetManifestGroupVersion(manifest, "OSLogging") },
        { "UnoCoreLoggingVersion", GetManifestGroupVersion(manifest, "CoreLogging") },
        { "UnoDspTasksVersion", GetManifestGroupVersion(manifest, "Dsp") },
    };

    var targets = CreateMSBuildFile(props);

    Console.WriteLine("Creating Uno.Sdk.Updater.targets for the Templates to sync with the Sdk Versions.");
    var outputPath = Path.Combine(LocalFileSystem.TemplatesSourceDirectory, "Uno.Sdk.Updater.targets");
    File.WriteAllText(outputPath, targets);
}

static string CreateMSBuildFile(IDictionary<string, string> props, params MsBuildItem[] items)
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

static string GetManifestGroupVersion(IEnumerable<ManifestGroup> manifest, string groupId)
{
    var group = manifest.First(x => x.Group == groupId);
    return group.Version;
}

static string GetManifestGroupVersionOverride(IEnumerable<ManifestGroup> manifest, string groupId, string overrideKey)
{
    var group = manifest.First(x => x.Group == groupId);
    if (group.VersionOverride is not null && group.VersionOverride.Count != 0)
    {
        return group.VersionOverride[overrideKey];
    }

    throw new InvalidOperationException($"No Version Overrides were fround for {groupId} or the key {overrideKey}.");
}

static async Task<ManifestGroup> UpdateGroup(ManifestGroup group, NuGetVersion unoVersion, NuGetApiClient client)
{
    if (group.Packages.Any(x => x.StartsWith("Xamarin")) || group.Group == "Core")
    {
        Console.WriteLine("Leaving group as is: " + group.Group);
        return group;
    }

    var preview = unoVersion.IsPreview;
    string[] stableOnlyGroups = [
    "CoreLogging",
    "OSLogging",
    "UniversalImageLoading",
    "WasmBootstrap"
];
    if (stableOnlyGroups.Any(x => x == group.Group))
    {
        preview = false;
    }
    else if (group.Group == "Prism")
    {
        preview = true;
    }
    else if (!group.Packages.Any(x => x.StartsWith("Uno.")))
    {
        preview = false;
    }

    var packageId = group.Packages.First();

    var version = await client.GetVersionAsync(packageId, preview);
    var newGroup = group with { Version = version };

    if (group.Version != newGroup.Version)
    {
        Console.WriteLine($"Updated Group '{group.Group}' to version '{newGroup.Version}'.");
    }

    if (group.VersionOverride is not null && group.VersionOverride.Count != 0)
    {
        var updatedOverrides = new Dictionary<string, string>();
        foreach((var key, var versionOverrideString) in group.VersionOverride)
        {
            if (!NuGetVersion.TryParse(versionOverrideString, out var versionOverride))
            {
                Console.WriteLine($"Could not parse version '{versionOverrideString} for {group.Group}.");
                continue;
            }

            version = await client.GetVersionAsync(packageId, versionOverride.IsPreview, versionOverride.OriginalVersion);
            if (version != versionOverrideString)
            {
                Console.WriteLine($"Updated Version Override for '{group.Group}' - '{key}' to '{version}'.");
            }
            updatedOverrides.Add(key, version);
        }

        newGroup = newGroup with { VersionOverride = updatedOverrides };
    }

    return newGroup;
}

internal record MsBuildItem(string Include, IDictionary<string, string> Attributes, string ItemType = "None")
{
    public string ToXml()
    {
        var attributeList = Attributes.Select(x => $"{x.Key}=\"{x.Value}\"");
        var attributes = string.Join(" ", attributeList);
        return $"<{ItemType} Include=\"{Include}\" {attributes} />";
    }
}