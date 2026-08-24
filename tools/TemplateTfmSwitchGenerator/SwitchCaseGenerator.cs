namespace TemplateTfmSwitchGenerator;

/// <summary>
/// Expands every platform combination into the switch cases consumed by the
/// <c>singleProjectTfms</c> generated symbol.
/// </summary>
public static class SwitchCaseGenerator
{
    public static IReadOnlyList<TemplateSwitchCase> Generate(Platform[] platforms, string[] runtimes)
    {
        var cases = new List<TemplateSwitchCase>();
        foreach (var runtime in runtimes)
        {
            GenerateCases(platforms, 0, $"tfm == '{runtime}' && ", string.Empty, cases, runtime);
        }

        return cases;
    }

    private static void GenerateCases(Platform[] platforms, int index, string currentCondition, string currentTfm, List<TemplateSwitchCase> cases, string runtime)
    {
        if (index == platforms.Length)
        {
            var finalizedCondition = $"({currentCondition.Trim(' ', '&')})";
            cases.Add(new TemplateSwitchCase(finalizedCondition, currentTfm.TrimEnd(';', ' ')));
            return;
        }

        var trueCondition = platforms[index].TrueCondition;
        var falseCondition = platforms[index].FalseCondition;
        var trueTfm = platforms[index].GetTfm(runtime) + ";";

        GenerateCases(platforms, index + 1, $"{currentCondition}{trueCondition} && ", $"{currentTfm}{trueTfm}", cases, runtime);
        GenerateCases(platforms, index + 1, $"{currentCondition}{falseCondition} && ", currentTfm, cases, runtime);
    }
}
