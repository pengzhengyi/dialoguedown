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
        var line = Line(speaker, speech);

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
        var option = RandomOption(NumberWeight(50), Line(Text("heads")));
        var random = RandomChoices(option);

        Assert.Equal([option], random.Children());
    }

    [Fact]
    public void Children_RandomOption_YieldsWeightThenBody()
    {
        var weight = AutoWeight();
        var body = Line(Text("body"));
        var option = RandomOption(weight, body);

        Assert.Equal([weight, body], option.Children());
    }

    [Fact]
    public void Children_ControlBlock_YieldsBranches()
    {
        var ifBranch = Branch(Condition("Rich"), Line(Text("wealthy")));
        var elseBranch = ElseBranch(Line(Text("broke")));
        var control = ControlBlock(ifBranch, elseBranch);

        Assert.Equal([ifBranch, elseBranch], control.Children());
    }

    [Fact]
    public void Children_Branch_YieldsConditionThenBody()
    {
        var condition = Condition("Rich");
        var body = Line(Text("wealthy"));
        var branch = Branch(condition, body);

        Assert.Equal([condition, body], branch.Children());
    }

    [Fact]
    public void Children_ElseBranch_YieldsBodyOnly()
    {
        var body = Line(Text("broke"));
        var branch = ElseBranch(body);

        Assert.Equal([body], branch.Children());
    }

    [Fact]
    public void Children_Jump_YieldsConditionThenLabel()
    {
        var condition = Condition("Rainy");
        var label = Text("Go");
        var jump = ConditionalJump(condition, "#a", label);

        Assert.Equal([condition, label], jump.Children());
    }

    [Fact]
    public void Children_SceneHeading_YieldsTitle()
    {
        var title = Text("Scene");
        var heading = SceneHeading(1, title);

        Assert.Equal([title], heading.Children());
    }

    [Fact]
    public void Children_SpeakerDeclaration_YieldsTags()
    {
        var tag = CustomTag("main");
        var declaration = SpeakerDeclaration("Alice", "A", tag);

        Assert.Equal([tag], declaration.Children());
    }

    [Fact]
    public void Children_PartialSpeakerDeclaration_YieldsTags()
    {
        var tag = CustomTag("excited");
        var partial = PartialSpeakerDeclaration("A", tag);

        Assert.Equal([tag], partial.Children());
    }

    [Fact]
    public void Children_StyledText_YieldsChildren()
    {
        var inner = Text("bold");
        var styled = StyledText(SpeechStyle.Bold, inner);

        Assert.Equal([inner], styled.Children());
    }

    [Fact]
    public void Children_Image_YieldsAlt()
    {
        var alt = Text("alt");
        var image = Image("a.png", alt);

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
            Query("hp"),
            DefaultCommand("wait"),
            CustomCommand("shake"),
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
    public void Children_UnhandledFragmentType_Throws()
    {
        var fragment = new UnknownFragment(SourceSpanFactory.Span());

        Assert.Throws<ArgumentOutOfRangeException>(() => fragment.Children());
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
        var jump = Jump("#play", go);
        var line = Line(speaker, hi, jump);

        Assert.Equal([line, speaker, hi, jump, go], line.DescendantsAndSelf());
    }

    [Fact]
    public void DescendantsAndSelf_LeafNode_YieldsOnlyItself()
    {
        var leaf = Text("x");

        Assert.Equal([leaf], leaf.DescendantsAndSelf());
    }

    [Fact]
    public void Texts_ReachesTheTextsHeldInsideOtherFragments_InDocumentOrder()
    {
        // Take **the ![old](key.png) key** to [the inn](#the-inn).
        var take = Text("Take ");
        var the = Text("the ");
        var old = Text("old");
        var key = Text(" key");
        var to = Text(" to ");
        var inn = Text("the inn");
        var stop = Text(".");
        IReadOnlyList<InlineFragment> fragments =
        [
            take,
            StyledText(SpeechStyle.Bold, the, Image("key.png", old), key),
            to,
            Link("#the-inn", inn),
            stop,
        ];

        Assert.Equal([take, the, old, key, to, inn, stop], fragments.Texts());
    }

    [Fact]
    public void Texts_SkipsFragmentsThatAreNotText()
    {
        var words = Text("Wait.");
        IReadOnlyList<InlineFragment> fragments =
            [words, LineBreak(), DefaultCommand("fade out"), CustomTag("calm")];

        Assert.Equal([words], fragments.Texts());
    }

    [Fact]
    public void PlainText_ConcatenatesEveryTextFragment()
    {
        IReadOnlyList<InlineFragment> fragments =
            [Text("Play "), StyledText(SpeechStyle.Bold, Text("tennis"))];

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

    [Fact]
    public void Jumps_ALine_FindsTheJumpItEndsIn()
    {
        var jump = Jump("#the-market", Text("east"));
        var line = Line(Text("Away. "), jump);

        Assert.Equal([jump], line.Jumps());
    }

    [Fact]
    public void Jumps_AControlLine_FindsTheJumpAmongItsEffects()
    {
        // A jump sits in a different place in each kind of block; in a control line it is among
        // the effects.
        var jump = Jump("#the-market", Text("east"));
        var control = ControlLine(DefaultCommand("fade out"), jump);

        Assert.Equal([jump], control.Jumps());
    }

    [Fact]
    public void Jumps_SeveralOnOneBlock_ComeInTheOrderTheyAreWritten()
    {
        var first = Jump("#first", Text("first"));
        var second = Jump("#second", Text("second"));

        Assert.Equal([first, second], Line(first, second).Jumps());
    }

    [Fact]
    public void Jumps_ABlockThatLeavesByNoJump_FindsNone()
    {
        Assert.Empty(Line(Text("Just speech.")).Jumps());
        Assert.Empty(ControlLine(DefaultCommand("fade out")).Jumps());
    }

    [Fact]
    public void Jumps_ABlockThatCannotCarryOne_FindsNone()
    {
        // A heading and a choice group are not places a jump is written, so asking is answered
        // rather than refused: a caller sweeping every block should not have to know which is which.
        Assert.Empty(SceneHeading().Jumps());
        Assert.Empty(Choices(Choice(Line(Text("pick")))).Jumps());
    }

    [Fact]
    public void Shown_ALine_IsWhatItSaysWithoutTheJumpItEndsIn()
    {
        var words = Text("Follow me. ");
        var line = Line(words, Jump("#the-market", Text("the market")));

        Assert.Equal([words], line.Shown());
    }

    [Fact]
    public void Shown_AJump_IsItsLabel()
    {
        var label = Text("the market");
        var jump = Jump("#the-market", label);

        Assert.Equal([label], jump.Shown());
    }

    [Fact]
    public void Shown_AnOption_IsNothing_ItsLabelIsShownByItsLine()
    {
        var line = Line(Text("I'll wait here."));
        var option = Choice(line);

        Assert.Empty(option.Shown());
        Assert.Equal(option.Label(), line.Shown());
    }

    [Fact]
    public void Shown_ANodeThePlayerNeverSees_IsNothing()
    {
        Assert.Empty(SceneHeading("The Tavern").Shown());
        Assert.Empty(ControlLine(DefaultCommand("fade out")).Shown());
    }

    // Script nodes the traversal helpers do not recognize, so each category's dispatch can be
    // shown to throw on an unhandled type.
    private sealed record UnknownNode(SourceSpan Span) : ScriptNode(Span);

    private sealed record UnknownBlock(SourceSpan Span) : ScriptBlock(Span);

    private sealed record UnknownSpeaker(SourceSpan Span) : Speaker(Span);

    private sealed record UnknownFragment(SourceSpan Span) : InlineFragment(Span);
}
