namespace Uno.Sdk.Updater.Utils
{
    /// Minimal helper to retrieve a CLI argument value (e.g. --exclude-file).
    internal static class Cli
    {
        public static string? GetArgValue(string name)
        {
            var av = Environment.GetCommandLineArgs();
            for (int i = 0; i < av.Length; i++)
            {
                if (string.Equals(av[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < av.Length)
                    return av[i + 1];
            }
            return null;
        }
    }
}
