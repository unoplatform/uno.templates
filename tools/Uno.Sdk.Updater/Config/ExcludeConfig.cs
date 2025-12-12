namespace Uno.Sdk.Updater.Config
{
    // Exclusions (JSON via --exclude-file)
    internal static class ExcludeConfig
    {
        public static string? ExcludeFilePath { get; set; }

        private static readonly Lazy<HashSet<string>> _excluded = new(() => Load());
        public static HashSet<string> Excluded => _excluded.Value;

        private static HashSet<string> Load()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var path = ExcludeFilePath;
            if (string.IsNullOrWhiteSpace(path))
                return set; // no exclusions when not provided

            // Normalize relative path
            if (!Path.IsPathRooted(path))
                path = Path.GetFullPath(path);

            if (!File.Exists(path))
            {
                Console.WriteLine($"Exclude file not found: {path}");
                return set;
            }

            try
            {
                using var fs = File.OpenRead(path);
                using var doc = System.Text.Json.JsonDocument.Parse(fs);
                if (doc.RootElement.TryGetProperty("exclude", out var arr) &&
                    arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var s = el.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                                set.Add(s);
                        }
                    }
                }

                Console.WriteLine($"Loaded {set.Count} exclusion(s) from {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read exclude file: {ex.Message}");
            }

            return set;
        }
    }
}
