using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Parsers;

/// <summary>
/// Answers whether plain text begins with a speaker prefix, by running the same
/// <see cref="SpeakerPrefixParser"/> grammar the transpiler uses to recognize one.
/// </summary>
internal static class SpeakerPrefixProbe
{
    /// <summary>
    /// Whether <paramref name="text"/> starts with a speaker prefix that names a speaker by
    /// name or id, or binds tags — the non-empty prefix the transpiler would recognize. A
    /// bare colon (an empty prefix that names no one) does not count.
    /// </summary>
    public static bool BeginsWithSpeakerPrefix(string text)
    {
        var result = SpeakerPrefixParser.Prefix.Consume(new ParseInput(text, 0));
        return result.Success && NamesSomething(result.MatchedValue);
    }

    private static bool NamesSomething(SpeakerPrefixData data) =>
        data.Name is not null || data.Id is not null || data.Tags.Count > 0;
}
