using System.Text;

namespace TemplateTfmSwitchGenerator;

/// <summary>
/// Reads and writes UTF-8 text files, preserving whether the original carried a BOM.
/// </summary>
public static class TextFile
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    public static (string Text, bool HasBom) Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

        return (Encoding.UTF8.GetString(hasBom ? bytes.AsSpan(3) : bytes), hasBom);
    }

    public static void Write(string path, string text, bool hasBom) =>
        File.WriteAllText(path, text, hasBom ? Utf8Bom : Utf8NoBom);
}
