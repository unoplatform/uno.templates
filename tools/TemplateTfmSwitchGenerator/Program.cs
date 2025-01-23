using System.Text.Json;
using TemplateTfmSwitchGenerator;

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true,
};
Platform[] platforms = [
    new Platform("platforms == desktop", "platforms != desktop", "desktop"),
    new Platform("platforms == android", "platforms != android", "android"),
    new Platform("platforms == ios", "platforms != ios", "ios"),
    new Platform("platforms == maccatalyst", "platforms != maccatalyst", "maccatalyst"),
    new Platform("platforms == windows", "platforms != windows", "windows10.0.26100"),
    new Platform("platforms == wasm", "platforms != wasm", "browserwasm"),
    new Platform("useUnitTests == true", "useUnitTests == false", null)
];

string[] runtimes = ["net8.0", "net9.0"];

var cases = new List<TemplateSwitchCase>();
foreach (var runtime in runtimes)
{
    var results = GenerateTemplateSwitchCases(platforms, runtime);
    cases.AddRange(results);
}

var json = JsonSerializer.Serialize(cases, options)
    .Replace(@"\u0026", "&")
    .Replace(@"\u0027", "'");
File.WriteAllText("template.json", json);
Console.WriteLine(json);

static IEnumerable<TemplateSwitchCase> GenerateTemplateSwitchCases(Platform[] platforms, string runtime)
{
    var cases = new List<TemplateSwitchCase>();
    var initialCondition = $"tfm == '{runtime}' && ";
    GenerateCases(platforms, 0, initialCondition, "", cases, runtime);
    return cases;
}

static void GenerateCases(Platform[] platforms, int index, string currentCondition, string currentTfm, List<TemplateSwitchCase> cases, string runtime)
{
    if (index == platforms.Length)
    {
        var finalizedCondition = $"({currentCondition.Trim(' ', '&')})";
        cases.Add(new TemplateSwitchCase(finalizedCondition, currentTfm.TrimEnd(';', ' ')));
        return;
    }

    string trueCondition = platforms[index].TrueCondition;
    string falseCondition = platforms[index].FalseCondition;
    string trueTfm = platforms[index].GetTfm(runtime) + ";";

    // Include true condition
    GenerateCases(platforms, index + 1, $"{currentCondition}{trueCondition} && ", $"{currentTfm}{trueTfm}", cases, runtime);

    // Include false condition
    GenerateCases(platforms, index + 1, $"{currentCondition}{falseCondition} && ", currentTfm, cases, runtime);
}