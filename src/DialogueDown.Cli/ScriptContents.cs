using System.Text;

namespace DialogueDown.Cli;

/// <summary>
/// A script's text as read from disk, remembering whether the file carried a UTF-8 byte order
/// mark so a rewrite can put it back. <see cref="Text"/> is the BOM-stripped text that diagnostic
/// offsets index, so the flag describes the file's bytes, not its locations.
/// </summary>
internal sealed record ScriptContents(string Text, bool HasBom)
{
    private static readonly byte[] _bom = [0xEF, 0xBB, 0xBF];

    /// <summary>Reads the script at <paramref name="path"/>.</summary>
    public static ScriptContents Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var hasBom = bytes.Length >= _bom.Length
            && bytes[0] == _bom[0]
            && bytes[1] == _bom[1]
            && bytes[2] == _bom[2];
        return new ScriptContents(
            Encoding.UTF8.GetString(bytes, hasBom ? _bom.Length : 0, bytes.Length - (hasBom ? _bom.Length : 0)),
            hasBom);
    }

    /// <summary>Writes the text back to <paramref name="path"/>, restoring its BOM when it had one.</summary>
    public void Write(string path) =>
        File.WriteAllText(path, Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: HasBom));
}
