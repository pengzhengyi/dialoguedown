using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Script.Transpiler.Parsers;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds transpiler builders for tests with their standard parser and builder
/// dependencies wired in one place.
/// </summary>
internal static class TranspilerBuilderFactory
{
    public static TagBuilder TagBuilder() => new();

    public static GameCallBuilder GameCallBuilder() => new(GameCallParser.Grammar);

    public static SpeakerBuilder SpeakerBuilder() =>
        new(SpeakerPrefixParser.Prefix, TagBuilder());

    public static InlineLeafBuilder InlineLeafBuilder() => new(TagBuilder());

    public static InlineBuilder InlineBuilder() => InlineBuilder(new LiteralInlinePolicy());

    public static InlineBuilder InlineBuilder(IInlinePolicy labelPolicy) =>
        new(InlineLeafBuilder(), GameCallBuilder(), labelPolicy);

    public static LineBuilder LineBuilder() => new(SpeakerBuilder(), InlineBuilder());

    public static BlockBuilder BlockBuilder() => new(InlineBuilder(), LineBuilder());

    public static ControlBlockBuilder ControlBlockBuilder() => new(BlockBuilder());

    public static ScriptTranspiler ScriptTranspiler() => new(BlockBuilder());
}
