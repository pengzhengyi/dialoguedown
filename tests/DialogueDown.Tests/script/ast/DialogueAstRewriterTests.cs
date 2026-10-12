using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class DialogueAstRewriterTests
{
    [Fact]
    public void Identity_PreservesFragmentStructure()
    {
        var line = AssertLine(new IdentityRewriter().Rewrite(FragmentSample()).Body[1]);

        AssertText(line.Speech[0], "hi ");
        AssertSingleText(AssertStyledText(line.Speech[1], SpeechStyle.Bold).Children, "bold");
        AssertSingleText(AssertLink(line.Speech[2], "#x").Label, "link");
        AssertSingleText(AssertImage(line.Speech[3], "p.png").Alt, "alt");
        AssertSingleText(AssertJump(line.Speech[4], "#y").Label, "jump");
        AssertCustomTag(line.Speech[5], "aside");
    }

    [Fact]
    public void Identity_PreservesEverySpeakerForm()
    {
        var body = new IdentityRewriter().Rewrite(SpeakerSample()).Body;

        AssertSpeakerDeclaration(AssertLine(body[0]).Speaker, "alice", "A", CustomTag("mood"));
        AssertPartialSpeakerDeclaration(AssertLine(body[1]).Speaker, "B", CustomTag("added"));
        AssertSpeakerNameReference(AssertLine(body[2]).Speaker, "bob");
        Assert.Null(AssertLine(body[3]).Speaker);
    }

    [Fact]
    public void Override_RewriteFragment_ReachesEveryFragmentSequence()
    {
        var result = new UppercaseTextRewriter().Rewrite(FragmentSample());

        // The one hook uppercases text in the heading title, the speech, every nested
        // label, and the nested choice body — no fragment sequence is missed.
        AssertSceneHeading(result.Body[0], "CAVE", 2);
        var line = AssertLine(result.Body[1]);
        AssertText(line.Speech[0], "HI ");
        AssertSingleText(AssertStyledText(line.Speech[1]).Children, "BOLD");
        AssertSingleText(AssertLink(line.Speech[2], "#x").Label, "LINK");
        AssertSingleText(AssertImage(line.Speech[3], "p.png").Alt, "ALT");
        AssertSingleText(AssertJump(line.Speech[4], "#y").Label, "JUMP");
        var choice = Assert.Single(Assert.IsType<Choices>(result.Body[2]).Options);
        AssertText(AssertLine(Assert.Single(choice.Body)).Speech[0], "CHOICE");
    }

    [Fact]
    public void Override_RewriteTag_ReachesSpeakerPrefixAndSpeech()
    {
        var body = new UppercaseTagRewriter().Rewrite(SpeakerSample()).Body;

        // A tag in a speaker declaration and a tag in speech both route through RewriteTag.
        AssertSpeakerDeclaration(AssertLine(body[0]).Speaker, "alice", "A", CustomTag("MOOD"));
        AssertCustomTag(AssertLine(body[0]).Speech[1], "ASIDE");
        AssertPartialSpeakerDeclaration(AssertLine(body[1]).Speaker, "B", CustomTag("ADDED"));
    }

    [Fact]
    public void UnknownBlockKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IdentityRewriter().Rewrite(
                Document(new UnknownScriptBlock(SourceSpanFactory.Span()))));

    [Fact]
    public void Identity_PreservesRandomChoiceWeightsAndOptions()
    {
        var random = AssertRandomChoices(new IdentityRewriter().Rewrite(RandomChoiceSample()).Body[0]);

        AssertNumberWeight(random.Options[0], 50);
        AssertAutoWeight(random.Options[1]);
        AssertSpeechText(AssertRandomOptionLine(random.Options[0]), "heads");
        AssertSpeechText(AssertRandomOptionLine(random.Options[1]), "tails");
    }

    [Fact]
    public void Override_RewriteFragment_ReachesRandomChoiceOptionBodies()
    {
        var random = AssertRandomChoices(new UppercaseTextRewriter().Rewrite(RandomChoiceSample()).Body[0]);

        AssertSpeechText(AssertRandomOptionLine(random.Options[0]), "HEADS");
        AssertSpeechText(AssertRandomOptionLine(random.Options[1]), "TAILS");
    }

    [Fact]
    public void Identity_PreservesControlBlockBranchesAndGuards()
    {
        var control = AssertControlBlock(new IdentityRewriter().Rewrite(ControlBlockSample()).Body[0]);

        AssertCondition(control.Branches[0].Condition, "Rich");
        AssertSpeechText(AssertLine(control.Branches[0].Body[0]), "wealthy");
        Assert.Null(control.Branches[1].Condition);
        AssertSpeechText(AssertLine(control.Branches[1].Body[0]), "broke");
    }

    [Fact]
    public void Override_RewriteBlock_ReachesControlBlockBranchBodies()
    {
        var control = AssertControlBlock(new UppercaseTextRewriter().Rewrite(ControlBlockSample()).Body[0]);

        AssertSpeechText(AssertLine(control.Branches[0].Body[0]), "WEALTHY");
        AssertSpeechText(AssertLine(control.Branches[1].Body[0]), "BROKE");
    }

    /// <summary>A control block with a conditional <c>if</c> branch and a bare <c>else</c>.</summary>
    /// <remarks>
    /// <code>
    /// > `if` `Rich?`
    /// >
    /// > wealthy
    /// >
    /// > `else`
    /// >
    /// > broke
    /// </code>
    /// </remarks>
    private static ScriptDocument ControlBlockSample() =>
        Document(
            ControlBlock(
                Branch(Condition("Rich"), Line(Text("wealthy"))),
                ElseBranch(Line(Text("broke")))));

    /// <summary>A random choice with a numeric weight and an auto weight.</summary>
    /// <remarks>
    /// <code>
    /// - `50%` heads
    /// - `%` tails
    /// </code>
    /// </remarks>
    private static ScriptDocument RandomChoiceSample() =>
        Document(
            RandomChoices(
                RandomOption(NumberWeight(50), Line(Text("heads"))),
                RandomOption(AutoWeight(), Line(Text("tails")))));

    /// <summary>A scene heading, a line holding a fragment of every kind, and a choice.</summary>
    /// <remarks>
    /// Built by hand, because the transpiler never emits a jump inside speech: Desugar assembles
    /// it, and the rewriter also runs after Desugar. It reads roughly as:
    /// <code>
    /// ## cave
    ///
    /// alice: hi **bold** [link](#x) ![alt](p.png) => [jump](#y) #aside
    ///
    /// - choice
    /// </code>
    /// </remarks>
    private static ScriptDocument FragmentSample() =>
        Document(
            SceneHeading("cave", 2),
            Line(
                SpeakerNameReference("alice"),
                Text("hi "),
                StyledText(SpeechStyle.Bold, Text("bold")),
                Link("#x", Text("link")),
                Image("p.png", Text("alt")),
                Jump("#y", Text("jump")),
                CustomTag("aside")),
            Choices(Choice(Line(Text("choice")))));

    /// <summary>
    /// One line for each speaker form — a declaration, a partial declaration, a name reference,
    /// and none — with a tag in the first line's speech.
    /// </summary>
    /// <remarks>
    /// <code>
    /// alice @A #mood: hi #aside
    /// @B #added: yo
    /// bob: hey
    /// narrator text
    /// </code>
    /// </remarks>
    private static ScriptDocument SpeakerSample() =>
        Document(
            Line(SpeakerDeclaration("alice", "A", CustomTag("mood")), Text("hi "), CustomTag("aside")),
            Line(PartialSpeakerDeclaration("B", CustomTag("added")), Text("yo")),
            Line(SpeakerNameReference("bob"), Text("hey")),
            Line(Text("narrator text")));

    private sealed class IdentityRewriter : DialogueAstRewriter;

    private sealed class UppercaseTextRewriter : DialogueAstRewriter
    {
        protected override InlineFragment RewriteFragment(InlineFragment fragment) =>
            fragment is Text text
                ? new Text(text.Content.ToUpperInvariant(), text.Span)
                : base.RewriteFragment(fragment);
    }

    private sealed class UppercaseTagRewriter : DialogueAstRewriter
    {
        protected override Tag RewriteTag(Tag tag) =>
            tag is CustomTag custom
                ? new CustomTag(custom.Name.ToUpperInvariant(), custom.Value, custom.Span)
                : base.RewriteTag(tag);
    }
}
