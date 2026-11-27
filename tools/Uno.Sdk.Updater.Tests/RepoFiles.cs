using System.Text.Json;
using Uno.Sdk.Models;

namespace Uno.Sdk.Updater.Tests;

// Files of this repository that the tests check, such as the Uno.Sdk manifest the updater writes.
internal static class RepoFiles
{
    public static IEnumerable<ManifestGroup> LoadManifest()
    {
        var path = Find("src/Uno.Sdk/packages.json");
        return JsonSerializer.Deserialize<ManifestGroup[]>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Could not read {path}");
    }

    public static string Find(string relativePath)
    {
        // The test runs from tools/Uno.Sdk.Updater.Tests/bin/...: walk up to the repository root.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            // Path.Join, not Path.Combine: it never drops the directory, even for a rooted relativePath
            var path = Path.Join(dir.FullName, relativePath);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"{relativePath} was not found above the test directory.");
    }
}
