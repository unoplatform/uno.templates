using System.Text.Json.Serialization;

namespace Uno.Sdk.Models;

internal record ManifestGroup(
    [property: JsonPropertyName("group")]string Group,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("packages")] string[] Packages,
    [property: JsonPropertyName("versionOverride")] Dictionary<string, string>? VersionOverride = null);
