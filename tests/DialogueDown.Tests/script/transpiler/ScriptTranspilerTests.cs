using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Script.Transpiler;

public sealed class ScriptTranspilerTests
{
    private readonly ScriptTranspiler _transpiler = TranspilerBuilderFactory.ScriptTranspiler();

    [Fact]
    public void Transpile_WrapsTheBuiltBlocksInAScriptDocument()
    {
        var source =
            """
            # The Cave

            Alice: Hi
            """;
        var document = Document(Heading(1, Text("The Cave")), TextParagraph("Alice: Hi"));

        var script = _transpiler.Transpile(document, DiagnosticsContextFactory.Context(source));

        Assert.Equal(2, script.Body.Count);
        AssertSceneHeading(script.Body[0], "The Cave", 1);
        var line = AssertLine(script.Body[1]);
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hi");
    }

    [Fact]
    public void Transpile_EmptyDocument_HasEmptyBody() =>
        Assert.Empty(_transpiler.Transpile(Document(), DiagnosticsContextFactory.Context()).Body);

    [Fact]
    public void Transpile_NullDocument_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _transpiler.Transpile(null!, DiagnosticsContextFactory.Context("source")));

    [Fact]
    public void Transpile_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _transpiler.Transpile(Document(), null!));

    [Fact]
    public void Transpile_AParsedScript_ProducesTheDialogueTree()
    {
        var source =
            """
            # The Cave

            Alice: Hello there.

            - Go left
            - Go right
            """;
        var script = Transpile(source);

        Assert.Equal(3, script.Body.Count);
        AssertSceneHeading(script.Body[0], "The Cave", 1);
        var line = AssertLine(script.Body[1]);
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hello there.");
        var choices = AssertChoices(script.Body[2], isOrdered: false);
        Assert.Equal(2, choices.Options.Count);
        AssertSpeechText(AssertChoiceLine(choices.Options[0]), "Go left");
        AssertSpeechText(AssertChoiceLine(choices.Options[1]), "Go right");
    }

    [Fact]
    public void Transpile_SpeechWithAnEscape_AnchorsTheTextSpanAtTheRealSource()
    {
        // "A: x \* y" — the backslash (index 5) escapes the star. Markdig splits the
        // speech into "x " and "* y"; the second fragment must anchor at the star's real
        // position (index 6), not drift onto the backslash.
        var source = @"A: x \* y";

        var script = Transpile(source);

        var speech = AssertLine(Assert.Single(script.Body)).Speech;
        var star = AssertText(speech[^1], "* y");
        Assert.Equal(source.IndexOf('*'), star.Span.Start);
        Assert.Equal('*', source[star.Span.Start]);
    }

    [Fact]
    public void Transpile_EscapedTag_ReadsAsPlainText()
    {
        // "\#notatag" — the front end records the escape, so the tag stays text.
        var script = Transpile(@"Alice: \#notatag");

        AssertSingleText(AssertSingleLine(script.Body).Speech, "#notatag");
    }

    [Fact]
    public void Transpile_EscapedReservedTag_ReadsAsPlainText()
    {
        var script = Transpile(@"Alice: \##default");

        AssertSingleText(AssertSingleLine(script.Body).Speech, "##default");
    }

    [Fact]
    public void Transpile_EscapedArrow_ReadsAsPlainText()
    {
        var script = Transpile(@"Alice: x \=> y");

        Assert.Collection(
            AssertSingleLine(script.Body).Speech,
            fragment => AssertText(fragment, "x "),
            fragment => AssertText(fragment, "=> y"));
    }

    private ScriptDocument Transpile(string source) =>
        _transpiler.Transpile(
            MarkdownParserFactory.Parse(source), DiagnosticsContextFactory.Context(source));
}
