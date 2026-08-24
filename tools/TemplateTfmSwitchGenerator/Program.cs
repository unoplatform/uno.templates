using TemplateTfmSwitchGenerator;

var checkOnly = args.Any(argument => argument is "--check");
var repoRoot = RepositoryLayout.FindRoot();
var outdated = new List<string>();

void Apply(string path, string original, string updated, bool hasBom)
{
    if (string.Equals(original, updated, StringComparison.Ordinal))
    {
        return;
    }

    outdated.Add(Path.GetRelativePath(repoRoot, path));

    if (!checkOnly)
    {
        TextFile.Write(path, updated, hasBom);
    }
}

var templateJsonPath = RepositoryLayout.Resolve(repoRoot, RepositoryLayout.SingleProjectTemplateJson);
var (templateJson, templateJsonHasBom) = TextFile.Read(templateJsonPath);
var cases = SwitchCaseGenerator.Generate(TemplateTfms.Platforms, TemplateTfms.Runtimes);

Apply(
    templateJsonPath,
    templateJson,
    TemplateJsonPatcher.PatchSwitchCases(templateJson, TemplateTfms.SingleProjectTfmsSymbol, cases),
    templateJsonHasBom);

foreach (var mirror in RepositoryLayout.EnumerateWindowsTfmMirrors(repoRoot))
{
    var (content, hasBom) = TextFile.Read(mirror);
    Apply(mirror, content, WindowsTfmRewriter.Rewrite(content, TemplateTfms.WindowsPlatformVersion), hasBom);
}

Console.WriteLine($"Windows platform version: {TemplateTfms.WindowsPlatformVersion}");
Console.WriteLine($"Runtimes: {string.Join(", ", TemplateTfms.Runtimes)}");
Console.WriteLine($"Generated {cases.Count} '{TemplateTfms.SingleProjectTfmsSymbol}' switch cases.");

if (outdated.Count == 0)
{
    Console.WriteLine("Template TFMs are up to date.");
    return 0;
}

foreach (var file in outdated)
{
    Console.WriteLine(checkOnly ? $"  out of date: {file}" : $"  updated: {file}");
}

if (checkOnly)
{
    Console.Error.WriteLine("Template TFMs are out of date. Run 'dotnet run --project tools/TemplateTfmSwitchGenerator' and commit the result.");
    return 1;
}

return 0;
