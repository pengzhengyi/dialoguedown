using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;
using EmphasisKind = DialogueDown.Markdown.EmphasisKind;
using MarkdownInline = DialogueDown.Markdown.MarkdownInline;
using TextInline = DialogueDown.Markdown.TextInline;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class LineBuilderTests
{
    private readonly LineBuilder _builder = TranspilerBuilderFactory.LineBuilder();

    [Fact]
    public void WithoutASpeaker_IsPlainSpeech()
    {
        var line = Build([Text("Hello there.")]);

        Assert.Null(line.Speaker);
        AssertSpeechText(line, "Hello there.");
    }

    [Fact]
    public void WithASpeaker_SplitsSpeakerFromSpeech()
    {
        var line = Build([Text("Alice: Hello there.")]);

        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hello there.");
    }

    [Fact]
    public void SpeakerFollowedByMoreInlines_KeepsThemAllInSpeech()
    {
        var line = Build(
            [Text("Alice: I said "), Emphasis(EmphasisKind.Italic, Text("hi"))]);

        AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Equal(2, line.Speech.Count);
        AssertText(line.Speech[0], "I said ");
        AssertStyledText(line.Speech[1], SpeechStyle.Italic);
    }

    [Fact]
    public void SpeakerWithNoSpeech_IsALineWithEmptySpeech()
    {
        var line = Build([Text("Alice:")]);

        AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Empty(line.Speech);
    }

    [Fact]
    public void LeadingNonText_HasNoSpeaker()
    {
        var line = Build([Emphasis(EmphasisKind.Bold, Text("Bang!"))]);

        Assert.Null(line.Speaker);
        AssertStyledText(Assert.Single(line.Speech), SpeechStyle.Bold);
    }

    [Fact]
    public void WithoutACondition_IsNotConditional()
    {
        var line = Build([Text("Alice: Hello there.")]);

        Assert.Null(line.Condition);
        Assert.False(line.IsConditional());
    }

    [Fact]
    public void LeadingCondition_IsPeeledOntoTheLineBeforeTheSpeaker()
    {
        var line = Build([CodeSpan("\"Angry\"?"), Text(" Guard: You again?")]);

        AssertCondition(line.Condition!, "Angry");
        Assert.True(line.IsConditional());
        AssertSpeakerNameReference(line.Speaker!, "Guard");
        AssertSpeechText(line, "You again?");
    }

    [Fact]
    public void LeadingCondition_WithoutASpeaker_KeepsTheSpeech()
    {
        var line = Build([CodeSpan("\"Returned\"?"), Text(" Welcome back.")]);

        AssertCondition(line.Condition!, "Returned");
        Assert.Null(line.Speaker);
        AssertSpeechText(line, "Welcome back.");
    }

    [Fact]
    public void OnlyACondition_IsNotPeeled_SoItStaysAnOrphanFragment()
    {
        // A condition with nothing after it guards nothing; it is left in speech for the
        // orphan-condition rule to report, rather than becoming an empty conditional line.
        var line = Build([CodeSpan("\"Angry\"?")]);

        Assert.Null(line.Condition);
        AssertCondition(Assert.Single(line.Speech), "Angry");
    }

    [Fact]
    public void LeadingConditionBeforeAJump_IsNotPeeled_SoItGuardsTheJump()
    {
        // `"key"?` => ... is a conditional *jump*: the condition stays in speech so desugar binds
        // it to the jump, rather than being stolen as the line's condition.
        var line = Build([CodeSpan("\"FoundKey\"?"), Text(" => "), Link("#the-vault", Text("open"))]);

        Assert.Null(line.Condition);
        Assert.Null(line.Speaker);
        AssertCondition(line.Speech[0], "FoundKey");
    }

    [Fact]
    public void MidSpeechCondition_IsNotACondition_AndStaysInSpeech()
    {
        // A condition that is not the leading inline is not a line condition; it stays in speech.
        var line = Build([Text("Guard: You "), CodeSpan("\"Angry\"?"), Text(" there")]);

        Assert.Null(line.Condition);
        AssertSpeakerNameReference(line.Speaker!, "Guard");
        AssertCondition(line.Speech[1], "Angry");
    }

    [Fact]
    public void TagsWithoutASpeakerName_ReportsAndUsesDefaultSpeaker()
    {
        var diagnostics = new DiagnosticBag();

        var line = _builder.Build([Text("#lonely: Hi")], diagnostics);

        Assert.IsType<DefaultSpeaker>(line.Speaker);
        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.TagsWithoutSpeaker);
    }

    [Fact]
    public void StyledLeadingText_IsNotASpeaker()
    {
        // `*Alice*:` parses as emphasis then ": ..." — the same shape as legitimate
        // emphasis-led prose (`*Note*: important`), so it is spoken, not a speaker.
        var line = Build(
            [Emphasis(EmphasisKind.Italic, Text("Alice")), Text(": Hello")]);

        Assert.Null(line.Speaker);
        AssertStyledText(line.Speech[0], SpeechStyle.Italic);
        AssertText(line.Speech[1], ": Hello");
    }

    [Fact]
    public void EscapedLeadingText_AnchorsSpeakerAndSpeechAtTheContentSpan()
    {
        // Mimics a leading literal whose backslash was stripped: the raw span starts at 0
        // (counting the '\'), but the content "Alice: hi" starts at 1. The speaker and the
        // leftover speech must anchor from the content, not the raw span.
        var leading = new TextInline("Alice: hi", Span(0, 10), Span(1, 9));

        var line = Build([leading]);

        var speaker = AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Equal(1, speaker.Span.Start); // "Alice" starts at the content, not at 0
        Assert.Equal(8, AssertText(Assert.Single(line.Speech), "hi").Span.Start); // 1 + "Alice: "
    }

    [Fact]
    public void EmptyGroup_Throws() =>
        Assert.Throws<ArgumentException>(() => Build([]));

    [Fact]
    public void Span_CoversTheWholeGroup()
    {
        var line = Build(
            [new TextInline("Hi ", Span(0, 3)), new TextInline("there", Span(3, 5))]);

        Assert.Equal(0, line.Span.Start);
        Assert.Equal(8, line.Span.End);
    }

    private Line Build(IReadOnlyList<MarkdownInline> group) =>
        _builder.Build(group, new DiagnosticBag());
}
