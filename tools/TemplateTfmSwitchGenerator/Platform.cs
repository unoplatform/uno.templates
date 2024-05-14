namespace TemplateTfmSwitchGenerator;

public record Platform(string TrueCondition, string FalseCondition, string? Runtime)
{
    public string GetTfm(string dotnetVersion)
    {
        if (string.IsNullOrEmpty(Runtime))
            return dotnetVersion;

        return $"{dotnetVersion}-{Runtime}";
    }
}