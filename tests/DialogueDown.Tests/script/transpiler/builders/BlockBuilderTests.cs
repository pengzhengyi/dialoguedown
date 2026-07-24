using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;
using MarkdownBlock = DialogueDown.Markdown.MarkdownBlock;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class BlockBuilderTests
{
    private readonly BlockBuilder _builder = TranspilerBuilderFactory.BlockBuilder();

    [Fact]
    public void EmptyDocument_HasEmptyBody() =>
        Assert.Empty(Build([]));

    [Fact]
    public void Heading_BecomesAFlatSceneHeading()
    {
        var heading = Heading(2, Text("The Cave"));

        var body = Build([heading]);

        var scene = AssertSceneHeading(Assert.Single(body), "The Cave", 2);
        Assert.Equal(heading.Span, scene.Span);
    }

    [Fact]
    public void Heading_ArrowIsPlainText_NotAJump()
    {
        // A heading becomes a scene, which is itself a jump target, so a `=>` inside a
        // heading is not a jump — it reads as plain text.
        var scene = Assert.IsType<SceneHeading>(
            Assert.Single(Build([Heading(1, Text("=> "), Link("#x", Text("there")))])));

        Assert.Equal(2, scene.Title.Count);
        AssertText(scene.Title[0], "=> ");
        AssertLink(scene.Title[1], "#x");
        Assert.DoesNotContain(scene.Title, fragment => fragment is JumpIndicator);
    }

    [Fact]
    public void Heading_StillRecognizesTagsAndGameCalls()
    {
        var scene = Assert.IsType<SceneHeading>(
            Assert.Single(Build([Heading(1, Text("Chapter #act1"), CodeSpan("\"n\""))])));

        AssertText(scene.Title[0], "Chapter ");
        AssertCustomTag(scene.Title[1], "act1");
        AssertQuery(scene.Title[2], "n");
    }

    [Fact]
    public void Paragraph_BecomesALine()
    {
        var body = Build([TextParagraph("Alice: Hello there.")]);

        var line = AssertLine(Assert.Single(body));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hello there.");
    }

    [Fact]
    public void Paragraph_HardBreak_SplitsIntoOneLinePerSlice()
    {
        var body = Build(
            [Paragraph(Text("Alice: Hi"), LineBreak(hard: true), Text("Bob: Yo"))]);

        Assert.Equal(2, body.Count);
        var first = AssertLine(body[0]);
        AssertSpeakerNameReference(first.Speaker!, "Alice");
        AssertSpeechText(first, "Hi");
        var second = AssertLine(body[1]);
        AssertSpeakerNameReference(second.Speaker!, "Bob");
        AssertSpeechText(second, "Yo");
    }

    [Fact]
    public void Paragraph_SoftBreak_StaysWithinOneLine()
    {
        var body = Build(
            [Paragraph(Text("Alice: Hi"), LineBreak(hard: false), Text("there"))]);

        var line = AssertLine(Assert.Single(body));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        Assert.Equal(3, line.Speech.Count);
        AssertText(line.Speech[0], "Hi");
        AssertLineBreak(line.Speech[1]);
        AssertText(line.Speech[2], "there");
    }

    [Fact]
    public void Paragraph_EmptyGroupsAroundHardBreaks_AreDropped()
    {
        var body = Build(
        [
            Paragraph(
                LineBreak(hard: true),                        // leading break -> empty group
                Text("Alice: Hi"),
                LineBreak(hard: true),
                LineBreak(hard: true),                        // doubled break -> empty group
                Text("Bob: Yo"),
                LineBreak(hard: true)),                       // trailing break -> empty group
        ]);

        Assert.Equal(2, body.Count);
        AssertSpeakerNameReference(AssertLine(body[0]).Speaker!, "Alice");
        AssertSpeakerNameReference(AssertLine(body[1]).Speaker!, "Bob");
    }

    [Fact]
    public void Paragraph_OnlyHardBreaks_EmitsNoLine() =>
        Assert.Empty(Build([Paragraph(LineBreak(hard: true))]));

    [Fact]
    public void List_BecomesChoices_OneChoicePerItem()
    {
        var body = Build(
        [
            ListBlock(
                ordered: false,
                ListItem(TextParagraph("Go north")),
                ListItem(TextParagraph("Go south"))),
        ]);

        var choices = AssertChoices(Assert.Single(body), isOrdered: false);
        Assert.Equal(2, choices.Options.Count);
        AssertSpeechText(AssertChoiceLine(choices.Options[0]), "Go north");
        AssertSpeechText(AssertChoiceLine(choices.Options[1]), "Go south");
    }

    [Fact]
    public void OrderedList_KeepsIsOrdered() =>
        AssertChoices(
            Assert.Single(Build([ListBlock(ordered: true, ListItem(TextParagraph("First")))])),
            isOrdered: true);

    [Fact]
    public void ChoiceItem_MayCarryASpeaker()
    {
        var body = Build([ListBlock(ordered: false, ListItem(TextParagraph("Alice: Hi")))]);

        var choices = AssertChoices(Assert.Single(body), isOrdered: false);
        var line = AssertChoiceLine(Assert.Single(choices.Options));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hi");
    }

    [Fact]
    public void ChoiceItem_WithMultipleBlocks_KeepsThemAll()
    {
        var body = Build(
        [
            ListBlock(
                ordered: false,
                ListItem(TextParagraph("Line one"), TextParagraph("Line two"))),
        ]);

        var choice = Assert.Single(AssertChoices(Assert.Single(body), isOrdered: false).Options);
        Assert.Equal(2, choice.Body.Count);
        AssertSpeechText(AssertLine(choice.Body[0]), "Line one");
        AssertSpeechText(AssertLine(choice.Body[1]), "Line two");
    }

    [Fact]
    public void NestedList_BecomesNestedChoices()
    {
        var innerList = ListBlock(ordered: false, ListItem(TextParagraph("Inner")));
        var outerList = ListBlock(
            ordered: false,
            ListItem(TextParagraph("Pick one:"), innerList));

        var body = Build([outerList]);

        // The outer list is one Choices with a single option.
        var choice = Assert.Single(AssertChoices(Assert.Single(body), isOrdered: false).Options);

        // That option holds its line, then the nested list as a nested Choices.
        Assert.Equal(2, choice.Body.Count);
        AssertSpeechText(AssertLine(choice.Body[0]), "Pick one:");
        var inner = AssertChoices(choice.Body[1], isOrdered: false);
        AssertSpeechText(AssertChoiceLine(Assert.Single(inner.Options)), "Inner");
    }

    [Fact]
    public void Choices_CarryListAndItemSpans()
    {
        var item = ListItem(TextParagraph("Go north"));
        var list = ListBlock(ordered: false, item);

        var choices = AssertChoices(Assert.Single(Build([list])), isOrdered: false);

        Assert.Equal(list.Span, choices.Span);
        Assert.Equal(item.Span, Assert.Single(choices.Options).Span);
    }

    [Fact]
    public void UnknownBlockKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Build([new UnknownMarkdownBlock(Span())]));

    [Fact]
    public void List_WithLeadingWeights_BecomesRandomChoices()
    {
        var body = Build(
        [
            ListBlock(
                ordered: false,
                ListItem(Paragraph(CodeSpan("50%"), Text(" Heads."))),
                ListItem(Paragraph(CodeSpan("50%"), Text(" Tails.")))),
        ]);

        var random = AssertRandomChoices(Assert.Single(body));
        Assert.Equal(2, random.Options.Count);
        Assert.Equal(new NumberWeight(50), random.Options[0].Weight);
        Assert.Equal(new NumberWeight(50), random.Options[1].Weight);
        AssertSpeechText(AssertRandomOptionLine(random.Options[0]), "Heads.");
        AssertSpeechText(AssertRandomOptionLine(random.Options[1]), "Tails.");
    }

    [Fact]
    public void RandomChoice_RecognizesNumberAndAutoWeights()
    {
        var body = Build(
        [
            ListBlock(
                ordered: false,
                ListItem(Paragraph(CodeSpan("70%"), Text(" Guard: Halt!"))),
                ListItem(Paragraph(CodeSpan("%"), Text(" Guard: Oh, it's you.")))),
        ]);

        var random = AssertRandomChoices(Assert.Single(body));
        Assert.Equal(new NumberWeight(70), random.Options[0].Weight);
        Assert.IsType<AutoWeight>(random.Options[1].Weight);
    }

    [Fact]
    public void RandomOption_WeightIsPeeled_AndTheSpeakerStillParses()
    {
        var body = Build(
        [
            ListBlock(ordered: false, ListItem(Paragraph(CodeSpan("50%"), Text(" Alice: Hi")))),
        ]);

        var line = AssertRandomOptionLine(Assert.Single(AssertRandomChoices(Assert.Single(body)).Options));
        AssertSpeakerNameReference(line.Speaker!, "Alice");
        AssertSpeechText(line, "Hi");
    }

    [Fact]
    public void RandomChoice_MissingWeightOnAnOption_ReportsMissingWeight_AndRecoversAsAuto()
    {
        var diagnostics = new DiagnosticBag();

        var body = _builder.Build(
        [
            ListBlock(
                ordered: false,
                ListItem(Paragraph(CodeSpan("50%"), Text(" Heads."))),
                ListItem(TextParagraph("Tails."))),
        ], diagnostics);

        var random = AssertRandomChoices(Assert.Single(body));
        Assert.Equal(new NumberWeight(50), random.Options[0].Weight);
        Assert.IsType<AutoWeight>(random.Options[1].Weight);
        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.MissingChoiceWeight);
    }

    [Fact]
    public void RandomChoice_InvalidWeight_ReportsInvalidWeight_AtItsSpan_AndRecoversAsAuto()
    {
        var diagnostics = new DiagnosticBag();
        var invalid = CodeSpan("-10%");

        var body = _builder.Build(
        [
            ListBlock(
                ordered: false,
                ListItem(Paragraph(invalid, Text(" Heads."))),
                ListItem(Paragraph(CodeSpan("%"), Text(" Tails.")))),
        ], diagnostics);

        var random = AssertRandomChoices(Assert.Single(body));
        Assert.IsType<AutoWeight>(random.Options[0].Weight);
        Assert.Equal(
            invalid.Span,
            AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.InvalidChoiceWeight).Span);
    }

    [Fact]
    public void RandomChoice_CarriesListAndItemSpans()
    {
        var item = ListItem(Paragraph(CodeSpan("50%"), Text(" Heads.")));
        var list = ListBlock(ordered: false, item);

        var random = AssertRandomChoices(Assert.Single(Build([list])));

        Assert.Equal(list.Span, random.Span);
        Assert.Equal(item.Span, Assert.Single(random.Options).Span);
    }

    private IReadOnlyList<ScriptBlock> Build(IReadOnlyList<MarkdownBlock> blocks) =>
        _builder.Build(blocks, new DiagnosticBag());
}
