using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsers;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// The production composition root for the default <see cref="IScriptTranspiler"/>: it
/// wires the builder graph (block, line, inline, speaker, game-call, tag) with their
/// standard parsers in one place, so a caller obtains a ready transpiler without knowing
/// the wiring.
/// </summary>
internal static class ScriptTranspilerFactory
{
    /// <summary>Creates the default transpiler with its standard builder graph.</summary>
    public static IScriptTranspiler CreateDefault() =>
        new ScriptTranspiler(new BlockBuilder(InlineBuilder(), new LineBuilder(SpeakerBuilder(), InlineBuilder())));

    private static InlineBuilder InlineBuilder() =>
        new(
            new InlineLeafBuilder(new TagBuilder()),
            new GameCallBuilder(GameCallParser.Grammar),
            new LiteralInlinePolicy());

    private static SpeakerBuilder SpeakerBuilder() =>
        new(SpeakerPrefixParser.Prefix, new TagBuilder());
}
