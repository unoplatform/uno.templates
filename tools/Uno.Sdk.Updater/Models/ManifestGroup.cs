using System.Text.Json.Serialization;

namespace Uno.Sdk.Models;

internal record ManifestGroup(
    [property: JsonPropertyName("group")]string Group,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("packages")] string[] Packages,
    [property: JsonPropertyName("versionOverride")] Dictionary<string, string>? VersionOverride = null,
    // The MSBuild property that overrides this group's version (for example UnoCSharpMarkupVersion).
    // Set by the Uno.Sdk.Private manifest; tools such as the canary NuGet updater read it from the
    // published Uno.Sdk. Must stay the last parameter so it is written as the last key.
    [property: JsonPropertyName("versionProperty")] string? VersionProperty = null);
