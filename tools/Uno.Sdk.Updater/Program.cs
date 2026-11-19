using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Uno.Sdk.Models;
using Uno.Sdk.Services;
using Uno.Sdk.Updater;
using Uno.Sdk.Updater.Config;
using Uno.Sdk.Updater.Utils;

const string UnoSdkPackageId = SdkFiles.SdkPackageId;

// Offline modes, no download: check (CI) or rewrite the files the updater derives from packages.json
// (ReadMe.md, Uno.Sdk.Updater.props, Uno.Sdk.Updater.targets).
var readMeTemplatePath = Path.Join(LocalFileSystem.UnoSdkUpdaterDirectory, SdkFiles.ReadMeFileName);
if (Environment.GetCommandLineArgs().Contains("--verify", StringComparer.OrdinalIgnoreCase))
{
    var drift = SdkFiles.FindDrift(LocalFileSystem.UnoSdkDirectory, LocalFileSystem.TemplatesSourceDirectory, readMeTemplatePath);
    if (drift.Count == 0)
    {
        Console.WriteLine("The Uno.Sdk files match packages.json.");
        return;
    }

    foreach (var message in drift)
    {
        Console.Error.WriteLine($"::error::{message}");
    }

    Console.Error.WriteLine("These files are written by the Uno.Sdk updater: don't edit them by hand. To rebuild them from packages.json, run:");
    Console.Error.WriteLine("  dotnet run --project tools/Uno.Sdk.Updater -- --regenerate");
    Environment.Exit(1);
}

if (Environment.GetCommandLineArgs().Contains("--regenerate", StringComparer.OrdinalIgnoreCase))
{
    var written = SdkFiles.Regenerate(LocalFileSystem.UnoSdkDirectory, LocalFileSystem.TemplatesSourceDirectory, readMeTemplatePath);
    Console.WriteLine(written.Count == 0 ? "The Uno.Sdk files already match packages.json." : $"Rewrote: {string.Join(", ", written)}");
    return;
}

Console.WriteLine("Starting the Uno.Sdk Updater");
Console.WriteLine($"Base Version: {UpdaterBuildContext.TemplateVersion}");
Console.WriteLine($"Minimum Search Version: {UpdaterBuildContext.MinVersion}");
Console.WriteLine($"Maximum Search Version: {UpdaterBuildContext.MaxVersion}");
WriteBreak();

// Minimal CLI parsing for --exclude-file
ExcludeConfig.ExcludeFilePath = Cli.GetArgValue("--exclude-file");

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    // We want to keep the output Human Readable
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

using var client = new NuGetApiClient();

Console.WriteLine($"Getting Package Versions for: {UnoSdkPackageId}");
var versions = await client.GetPackageVersions(UnoSdkPackageId);

versions = versions.Where(x => x > UpdaterBuildContext.MinVersion && x < UpdaterBuildContext.MaxVersion);

if (!versions.Any())
{
    Console.Error.WriteLine("No Uno Version was found.");

    // Ensure that we do not return a success exit code.
    throw new InvalidOperationException("No Uno Version was found.");
}

var unoVersion = versions.OrderByDescending(x => x).First();
client.UnoVersion = unoVersion;

Console.WriteLine($"Found Uno Version: {unoVersion}");
Console.WriteLine($"Downloading {UnoSdkPackageId}");
using var sdkPackage = await client.DownloadPackageAsync(UnoSdkPackageId, unoVersion);
using var sdkZip = new ZipArchive(sdkPackage);

string? readMePath = null;
string? packagesJsonPath = null;
string? relativePackagesJsonPath = null;
string? tags = null;
bool wroteChanges = false;

foreach (var entry in sdkZip.Entries)
{
    var extension = Path.GetExtension(entry.FullName);
    string[] allowedExtensions = [".md", ".json", ".nuspec"];
    if (!allowedExtensions.Any(x => x.Equals(extension, StringComparison.InvariantCultureIgnoreCase)) ||
        (extension.Equals(".json", StringComparison.InvariantCultureIgnoreCase) && !Path.GetFileName(entry.FullName).Equals("packages.json", StringComparison.InvariantCultureIgnoreCase)))
    {
        continue;
    }

    WriteBreak();

    Console.WriteLine($"Evaluating {entry.FullName}");
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
        var inputManifest = await JsonSerializer.DeserializeAsync<IEnumerable<ManifestGroup>>(packageStream)
            ?? throw new InvalidOperationException("Unable to parse the packages.json from the Sdk.");

        inputManifest = unoVersion.IsPreview
        ? MergeLocalOverridesOnly(inputManifest, outputPath)  // keep only local VersionOverride entries
        : MergeLocalManifest(inputManifest, outputPath);      // full merge as before

        var manifest = new List<ManifestGroup>();
        foreach (var group in inputManifest)
        {
            var updated = await UpdateGroup(group, unoVersion, client);
            manifest.Add(updated);
        }

        CreateUpdaterTargets(manifest, ref wroteChanges);
        var json = JsonSerializer.Serialize(manifest, jsonOptions);

        WriteIfDifferent(outputPath, json, ref wroteChanges);
    }
    else if (extension == ".nuspec")
    {
        Console.WriteLine("Extracting NuSpec Metadata");
        var nuspec = NuGet.Packaging.Manifest.ReadFrom(entry.Open(), false);
        tags = nuspec.Metadata.Tags;
    }
    else
    {
        Console.WriteLine($"Copying file: {outputPath}");
        entry.ExtractToFile(outputPath, true);
    }
}

WriteBreak();

if (wroteChanges && (string.IsNullOrEmpty(readMePath) || !File.Exists(readMePath)))
{
    Console.WriteLine($"The downloaded {UnoSdkPackageId} did not contain a ReadMe.md, using local template.");
    readMePath = Path.Combine(LocalFileSystem.UnoSdkDirectory, "ReadMe.md");
    File.Copy(Path.Combine(LocalFileSystem.UnoSdkUpdaterDirectory, "ReadMe.md"), readMePath, true);
    wroteChanges = true;
}

if (!string.IsNullOrEmpty(readMePath) && File.Exists(readMePath) &&
    !string.IsNullOrEmpty(packagesJsonPath) && File.Exists(packagesJsonPath))
{
    var readMe = SdkFiles.BuildReadMe(File.ReadAllText(readMePath), File.ReadAllText(packagesJsonPath));

    Console.WriteLine("Updated the ReadMe with the versions used by this pack of the Uno.Sdk.");
    WriteIfDifferent(readMePath, readMe, ref wroteChanges);

    CreateSdkProps(tags, unoVersion, readMePath, packagesJsonPath, relativePackagesJsonPath ?? "ERROR - Unable to determine path", ref wroteChanges);
}

if (wroteChanges)
{
    Console.WriteLine("Uno.Sdk has been updated.");
}
else
{
    Console.WriteLine("No changes were made to the Uno.Sdk.");
}

Console.WriteLine("Finished Uno.Sdk update.");

static IEnumerable<ManifestGroup> MergeLocalManifest(IEnumerable<ManifestGroup> sdkManifest, string packagesJsonPath)
    => ManifestMerge.MergeLocalManifest(sdkManifest, ReadLocalManifest(packagesJsonPath));

// Keep local versions as baseline. Only ensure local VersionOverride are present,
// and bring in any brand-new groups that exist in the SDK manifest.
static IEnumerable<ManifestGroup> MergeLocalOverridesOnly(IEnumerable<ManifestGroup> sdkManifest, string packagesJsonPath)
    => ManifestMerge.MergeLocalOverridesOnly(sdkManifest, ReadLocalManifest(packagesJsonPath));

static IEnumerable<ManifestGroup> ReadLocalManifest(string packagesJsonPath)
    => JsonSerializer.Deserialize<IEnumerable<ManifestGroup>>(File.ReadAllText(packagesJsonPath))
        ?? throw new InvalidOperationException($"Unable to parse local packages.json at '{packagesJsonPath}'.");

static void WriteIfDifferent(string filePath, string content, ref bool didWriteChanges)
{
    if (!File.Exists(filePath) || !File.ReadAllText(filePath).Equals(content))
    {
        File.WriteAllText(filePath, content, Encoding.UTF8);
        didWriteChanges = true;
        Console.WriteLine($"File '{filePath}' updated");
    }
    else
    {
        Console.WriteLine($"File '{filePath}' skipped. Already up to date.");
    }
}

static void CreateSdkProps(string? tags, string unoVersion, string readMePath, string packagesJsonPath, string relativePackagesJsonPath, ref bool didWriteChanges)
{
    Console.WriteLine("Creating Sdk Updater Props for the Uno.Sdk");
    var nuspecTargets = SdkFiles.BuildSdkProps(tags, unoVersion);
    WriteIfDifferent(Path.Combine(LocalFileSystem.UnoSdkDirectory, SdkFiles.UpdaterPropsFileName), nuspecTargets, ref didWriteChanges);
}

static void CreateUpdaterTargets(IEnumerable<ManifestGroup> manifest, ref bool didWriteChanges)
{
    var targets = SdkFiles.BuildUpdaterTargets(manifest);

    Console.WriteLine("Creating Uno.Sdk.Updater.targets for the Templates to sync with the Sdk Versions.");
    var outputPath = Path.Combine(LocalFileSystem.TemplatesSourceDirectory, SdkFiles.UpdaterTargetsFileName);
    WriteIfDifferent(outputPath, targets, ref didWriteChanges);
}

static async Task<ManifestGroup> UpdateGroup(ManifestGroup group, NuGetVersion unoVersion, NuGetApiClient client)
{
    if (ExcludeConfig.Excluded.Contains(group.Group))
    {
        Console.WriteLine($"Skipping '{group.Group}' due to exclusion list.");
        return group;
    }

    if (group.Group == "Core")
    {
        Console.WriteLine($"Setting Core group to: {unoVersion.OriginalVersion}");
        return group with { Version = unoVersion };
    }
    // AndroidX packages, and both MAUI groups on release branches (see UpdatePolicy)
    else if (UpdatePolicy.IsHeldToAvoidJavaMisalignment(group, unoVersion.IsPreview))
    {
        Console.WriteLine($"Skipping '{group.Group}' to avoid Java misalignment.");
        return group;
    }

    var preview = unoVersion.IsPreview;

    string[] stableOnlyGroups = [
        "CoreLogging",
        "OSLogging",
        "WasmBootstrap"
    ];

    if (stableOnlyGroups.Any(x => x == group.Group))
    {
        preview = false;
    }
    else if (!group.Packages.Any(x => x.StartsWith("Uno.")))
    {
        preview = false;
    }

    var packageId = group.Packages.FirstOrDefault(x => x.Contains("WinUI", StringComparison.InvariantCultureIgnoreCase) && x.Contains("Uno", StringComparison.InvariantCultureIgnoreCase)) ??
        group.Packages.First();

    var noMajorUpgrade = UpdatePolicy.MajorUpgradeDisabledGroups.Any(x => x == group.Group);
    var version = await client.GetVersionAsync(packageId, preview, noMajorUpgrade, group.Version);

    if (string.IsNullOrEmpty(version))
    {
        Console.WriteLine($"Unable to find package {packageId} versions, skipping.");
        version = "0.0.0.0";
    }

    version = !string.IsNullOrEmpty(group.Version) && NuGetVersion.Parse(version).IsOlderThan(NuGetVersion.Parse(group.Version)) ? group.Version : version;
    var newGroup = group with { Version = version };

    if (group.Version != newGroup.Version)
    {
        Console.WriteLine($"Updated Group '{group.Group}' to version '{newGroup.Version}'.");
    }

    if (group.VersionOverride is not null && group.VersionOverride.Count != 0)
    {
        var updatedOverrides = new Dictionary<string, string>();
        foreach ((var key, var versionOverrideString) in group.VersionOverride)
        {
            if (!NuGetVersion.TryParse(versionOverrideString, out var versionOverride))
            {
                Console.WriteLine($"Could not parse version '{versionOverrideString}' for group '{group.Group}', package '{packageId}'.");
                continue;
            }

            // A target framework override stays on its .NET major (net10.0 -> 10.x): otherwise the net10.0 entries
            // move to the .NET 11 builds (Uno.Wasm.Bootstrap 11.x; MicrosoftLoggingConsole and Maui once .NET 11 ships).
            version = await client.GetVersionAsync(packageId, versionOverride.IsPreview, false, versionOverride.OriginalVersion, GetTargetFrameworkMajor(key));

            if (!NuGetVersion.TryParse(version, out var parsedVersion))
            {
                Console.WriteLine($"Could not parse version '{version}' for {group.Group}.");
                // Fall back to the local override as a safe default
                version = versionOverride.OriginalVersion;
            }
            else if (parsedVersion.IsOlderThan(versionOverride))
            {
                // Keep the local override (e.g., 10.0.0-preview) if the feed returns a lower version (e.g., 9.x).
                // A release candidate pin still moves to its release (11.0.0-rc.3 -> 11.0.0).
                version = versionOverride.OriginalVersion;
            }

            // Logged after the checks above, which can put the local override back
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

// "net10.0" -> 10; null for keys that aren't a .NET target framework.
static int? GetTargetFrameworkMajor(string key)
{
    var match = Regex.Match(key, @"^net(\d+)\.\d+");
    return match.Success ? int.Parse(match.Groups[1].Value) : null;
}

static void WriteBreak()
{
    Console.WriteLine("----------------------------------------------------");
    Console.WriteLine();
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
