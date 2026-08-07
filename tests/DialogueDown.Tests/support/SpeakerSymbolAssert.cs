using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertion helpers for speaker symbols, so a test can state the tags it expects — by name
/// and value, in document order — instead of projecting and comparing them by hand.
/// </summary>
internal static class SpeakerSymbolAssert
{
    public static SpeakerSymbol AssertHasSpeaker(
        SemanticModel model,
        Speaker speaker,
        string expectedName)
    {
        var symbol = model.Speakers.Resolve(speaker);
        Assert.Equal(expectedName, symbol.Name);
        return symbol;
    }

    public static void AssertTags(SpeakerSymbol symbol, params (string Name, string? Value)[] expected) =>
        Assert.Equal(expected, symbol.Tags.Select(tag => (tag.Name, tag.Value)));
}
