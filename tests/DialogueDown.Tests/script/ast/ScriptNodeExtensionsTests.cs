using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ScriptNodeExtensionsTests
{
    [Fact]
    public void Children_Line_YieldsSpeakerThenSpeech()
    {
        var speaker = SpeakerNameReference("Alice");
        var speech = Text("hi");
        var line = new Line(speaker, [speech], SourceSpanFactory.Span());

        Assert.Equal([speaker, speech], line.Children());
    }

    [Fact]
    public void Children_Line_WithoutSpeaker_YieldsSpeechOnly()
    {
        var speech = Text("hi");
        var line = Line(speech); // factory builds a speaker-less line

        Assert.Equal([speech], line.Children());
    }

    [Fact]
    public void Children_Choices_YieldsOptions()
    {
        var option = Choice(Line(Text("pick")));
        var choices = Choices(option);

        Assert.Equal([option], choices.Children());
    }

    [Fact]
    public void Children_Choice_YieldsBody()
    {
        var body = Line(Text("body"));
        var choice = Choice(body);

        Assert.Equal([body], choice.Children());
    }

    [Fact]
    public void Children_RandomChoices_YieldsOptions()
    {
        var option = RandomOption(new NumberWeight(50), Line(Text("heads")));
        var random = RandomChoices(option);

        Assert.Equal([option], random.Children());
    }

    [Fact]
    public void Children_RandomOption_YieldsBody()
    {
        var body = Line(Text("body"));
        var option = RandomOption(new AutoWeight(), body);

        Assert.Equal([body], option.Children());
    }

    [Fact]
    public void Children_SceneHeading_YieldsTitle()
    {
        var title = Text("Scene");
        var heading = new SceneHeading([title], 1, SourceSpanFactory.Span());

        Assert.Equal([title], heading.Children());
    }

    [Fact]
    public void Children_SpeakerDeclaration_YieldsTags()
    {
        var tag = CustomTag("main");
        var declaration = new SpeakerDeclaration("Alice", "A", [tag], SourceSpanFactory.Span());

        Assert.Equal([tag], declaration.Children());
    }

    [Fact]
    public void Children_PartialSpeakerDeclaration_YieldsTags()
    {
        var tag = CustomTag("excited");
        var partial = new PartialSpeakerDeclaration("A", [tag], SourceSpanFactory.Span());

        Assert.Equal([tag], partial.Children());
    }

    [Fact]
    public void Children_StyledText_YieldsChildren()
    {
        var inner = Text("bold");
        var styled = new StyledText(SpeechStyle.Bold, [inner], SourceSpanFactory.Span());

        Assert.Equal([inner], styled.Children());
    }

    [Fact]
    public void Children_Image_YieldsAlt()
    {
        var alt = Text("alt");
        var image = new Image("a.png", [alt], SourceSpanFactory.Span());

        Assert.Equal([alt], image.Children());
    }

    [Fact]
    public void Children_Link_YieldsLabel()
    {
        var label = Text("label");
        var link = Link("#x", label);

        Assert.Equal([label], link.Children());
    }

    [Fact]
    public void Children_Jump_YieldsLabel()
    {
        var label = Text("go");
        var jump = Jump("#play", label);

        Assert.Equal([label], jump.Children());
    }

    [Fact]
    public void Children_LeafNodes_AreEmpty()
    {
        ScriptNode[] leaves =
        [
            Text("t"),
            LineBreak(),
            JumpIndicator(),
            DefaultSpeaker(),
            SpeakerNameReference("Alice"),
            SpeakerIdReference("A"),
            new Query("hp", SourceSpanFactory.Span()),
            new DefaultCommand("wait", SourceSpanFactory.Span()),
            new CustomCommand("shake", [], SourceSpanFactory.Span()),
            CustomTag("main"),
            ReservedTag("default"),
        ];

        Assert.All(leaves, leaf => Assert.Empty(leaf.Children()));
    }

    [Fact]
    public void Children_UnhandledNodeType_Throws()
    {
        var node = new UnknownNode(SourceSpanFactory.Span());

        Assert.Throws<ArgumentOutOfRangeException>(() => node.Children());
    }

    [Fact]
    public void Children_UnhandledBlockType_Throws()
    {
        var block = new UnknownBlock(SourceSpanFactory.Span());

        Assert.Throws<ArgumentOutOfRangeException>(() => block.Children());
    }

    [Fact]
    public void Children_UnhandledSpeakerType_Throws()
    {
        var speaker = new UnknownSpeaker(SourceSpanFactory.Span());

        Assert.Throws<ArgumentOutOfRangeException>(() => speaker.Children());
    }

    [Fact]
    public void TypeChainToScriptNode_YieldsConcreteTypeUpToScriptNode()
    {
        var node = SpeakerNameReference("Alice");

        Assert.Equal(
            [
                typeof(SpeakerNameReference),
                typeof(SpeakerReference),
                typeof(Speaker),
                typeof(ScriptNode),
            ],
            node.TypeChainToScriptNode());
    }

    [Fact]
    public void DescendantsAndSelf_YieldsSelfThenDescendants_InDocumentOrder()
    {
        var speaker = SpeakerNameReference("Alice");
        var hi = Text("hi");
        var go = Text("go");
        var jump = new Jump("#play", [go], SourceSpanFactory.Span());
        var line = new Line(speaker, [hi, jump], SourceSpanFactory.Span());

        Assert.Equal([line, speaker, hi, jump, go], line.DescendantsAndSelf());
    }

    [Fact]
    public void DescendantsAndSelf_LeafNode_YieldsOnlyItself()
    {
        var leaf = Text("x");

        Assert.Equal([leaf], leaf.DescendantsAndSelf());
    }

    [Fact]
    public void PlainText_ConcatenatesEveryTextFragment()
    {
        IReadOnlyList<InlineFragment> fragments =
            [Text("Play "), new StyledText(SpeechStyle.Bold, [Text("tennis")], SourceSpanFactory.Span())];

        Assert.Equal("Play tennis", fragments.PlainText());
    }

    [Fact]
    public void PlainText_ReadsThroughALinkLabel()
    {
        IReadOnlyList<InlineFragment> fragments = [Link("#x", Text("go"))];

        Assert.Equal("go", fragments.PlainText());
    }

    [Fact]
    public void PlainText_IgnoresNonTextFragments()
    {
        IReadOnlyList<InlineFragment> fragments = [Text("Chapter "), CustomTag("act1")];

        Assert.Equal("Chapter ", fragments.PlainText());
    }

    [Fact]
    public void PlainText_EmptySequence_IsEmpty() =>
        Assert.Equal(string.Empty, Array.Empty<InlineFragment>().PlainText());

    // Script nodes the traversal helpers do not recognize, used to prove each category's
    // dispatch throws on an unhandled type as the AST grows.
    private sealed record UnknownNode(SourceSpan Span) : ScriptNode(Span);

    private sealed record UnknownBlock(SourceSpan Span) : ScriptBlock(Span);

    private sealed record UnknownSpeaker(SourceSpan Span) : Speaker(Span);
}
