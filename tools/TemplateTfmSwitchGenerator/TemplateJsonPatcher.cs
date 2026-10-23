namespace TemplateTfmSwitchGenerator;

/// <summary>
/// Rewrites the <c>cases</c> array of a generated switch symbol in place, leaving the
/// rest of the template.json (formatting, comments and line endings) untouched.
/// </summary>
public static class TemplateJsonPatcher
{
    public static string PatchSwitchCases(string json, string symbolName, IReadOnlyList<TemplateSwitchCase> cases)
    {
        var newLine = json.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = json.Split(newLine).ToList();

        var symbolIndex = lines.FindIndex(line => line.TrimStart().StartsWith($"\"{symbolName}\":", StringComparison.Ordinal));
        if (symbolIndex < 0)
        {
            throw new InvalidOperationException($"Could not find the '{symbolName}' symbol in the template.json.");
        }

        var casesIndex = lines.FindIndex(symbolIndex, line => line.TrimStart().StartsWith("\"cases\":", StringComparison.Ordinal));
        if (casesIndex < 0)
        {
            throw new InvalidOperationException($"The '{symbolName}' symbol does not declare a 'cases' array.");
        }

        var indent = lines[casesIndex][..^lines[casesIndex].TrimStart().Length];
        var closingIndex = lines.FindIndex(casesIndex + 1, line => line.StartsWith($"{indent}]", StringComparison.Ordinal));
        if (closingIndex < 0)
        {
            throw new InvalidOperationException($"The 'cases' array of the '{symbolName}' symbol is not closed at the expected indentation.");
        }

        lines.RemoveRange(casesIndex + 1, closingIndex - casesIndex - 1);
        lines.InsertRange(casesIndex + 1, RenderCases(cases, indent));

        return string.Join(newLine, lines);
    }

    private static IEnumerable<string> RenderCases(IReadOnlyList<TemplateSwitchCase> cases, string indent)
    {
        var itemIndent = $"{indent}  ";
        var propertyIndent = $"{itemIndent}  ";

        for (var i = 0; i < cases.Count; i++)
        {
            var separator = i == cases.Count - 1 ? string.Empty : ",";
            yield return $"{itemIndent}{{";
            yield return $"{propertyIndent}\"condition\": \"{Escape(cases[i].Condition)}\",";
            yield return $"{propertyIndent}\"value\": \"{Escape(cases[i].Value)}\"";
            yield return $"{itemIndent}}}{separator}";
        }
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
