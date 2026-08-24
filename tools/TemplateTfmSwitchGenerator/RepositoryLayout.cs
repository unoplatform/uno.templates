namespace TemplateTfmSwitchGenerator;

/// <summary>
/// Locates the files the generator owns, relative to the repository root.
/// </summary>
public static class RepositoryLayout
{
    public const string SingleProjectTemplateJson = "src/Uno.Templates/content/unoapp/.template.config/template.json";

    private const string TemplateContentRoot = "src/Uno.Templates/content";

    private static readonly string[] MirrorExtensions = [".csproj", ".props", ".targets", ".xml", ".json", ".sln", ".slnx"];

    public static string FindRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Resolve(directory.FullName, SingleProjectTemplateJson)))
                {
                    return directory.FullName;
                }
            }
        }

        throw new InvalidOperationException($"Could not locate the repository root: no '{SingleProjectTemplateJson}' found above the generator.");
    }

    public static string Resolve(string repoRoot, string relativePath) =>
        Path.GetFullPath(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    /// Template content carrying a hand-written Windows TFM. The template.json is excluded
    /// because its switch cases are generated rather than rewritten.
    /// </summary>
    public static IEnumerable<string> EnumerateWindowsTfmMirrors(string repoRoot)
    {
        var contentRoot = Resolve(repoRoot, TemplateContentRoot);
        var templateJson = Resolve(repoRoot, SingleProjectTemplateJson);

        return Directory.EnumerateFiles(contentRoot, "*", SearchOption.AllDirectories)
            .Where(file => MirrorExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Where(file => !IsBuildOutput(contentRoot, file))
            .Where(file => !string.Equals(file, templateJson, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBuildOutput(string contentRoot, string file) =>
        Path.GetRelativePath(contentRoot, file)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj");
}
