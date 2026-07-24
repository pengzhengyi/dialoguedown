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

        AssertSpeakerDeclaration(AssertLine(body[0]).Speaker!, "alice", "A", CustomTag("mood"));
        AssertPartialSpeakerDeclaration(AssertLine(body[1]).Speaker!, "B", CustomTag("added"));
        AssertSpeakerNameReference(AssertLine(body[2]).Speaker!, "bob");
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
        AssertSpeakerDeclaration(AssertLine(body[0]).Speaker!, "alice", "A", CustomTag("MOOD"));
        AssertCustomTag(AssertLine(body[0]).Speech[1], "ASIDE");
        AssertPartialSpeakerDeclaration(AssertLine(body[1]).Speaker!, "B", CustomTag("ADDED"));
    }

    [Fact]
    public void UnknownBlockKind_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IdentityRewriter().Rewrite(
                new ScriptDocument([new UnknownScriptBlock(SourceSpanFactory.Span())])));

    [Fact]
    public void Identity_PreservesRandomChoiceWeightsAndOptions()
    {
        var random = AssertRandomChoices(new IdentityRewriter().Rewrite(RandomChoiceSample()).Body[0]);

        Assert.Equal(new NumberWeight(50), random.Options[0].Weight);
        Assert.Equal(new AutoWeight(), random.Options[1].Weight);
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

    // A random choice with a numeric and an auto weight, so both the weights and the option
    // bodies can be checked after a rewrite. Shape:
    //
    //   - `50%` heads
    //   - `%`   tails
    private static ScriptDocument RandomChoiceSample() =>
        new(
        [
            new RandomChoices(
                [
                    new RandomOption(new NumberWeight(50), [Line(Text("heads"))], SourceSpanFactory.Span()),
                    new RandomOption(new AutoWeight(), [Line(Text("tails"))], SourceSpanFactory.Span()),
                ],
                SourceSpanFactory.Span()),
        ]);

    // A hand-built tree holding a fragment of every kind, so a rewrite can be checked at
    // each position. Jump is included even though the transpiler never emits one directly
    // (Desugar assembles it), because the rewriter is reused after Desugar. Shape:
    //
    //   ## cave
    //   alice: hi **bold** [link](#x) ![alt](p.png) =>[jump](#y) #aside
    //   - choice
    private static ScriptDocument FragmentSample() =>
        new(
        [
            new SceneHeading([Text("cave")], 2, SourceSpanFactory.Span()),
            new Line(
                SpeakerNameReference("alice"),
                [
                    Text("hi "),
                    new StyledText(SpeechStyle.Bold, [Text("bold")], SourceSpanFactory.Span()),
                    new Link("#x", [Text("link")], SourceSpanFactory.Span()),
                    new Image("p.png", [Text("alt")], SourceSpanFactory.Span()),
                    Jump("#y", Text("jump")),
                    CustomTag("aside"),
                ],
                SourceSpanFactory.Span()),
            Choices(Choice(Line(Text("choice")))),
        ]);

    // One line per speaker form, plus a speech tag, so speaker and tag rewriting can be
    // checked in every place a tag or speaker appears. Shape:
    //
    //   alice @A #mood: hi #aside     (declaration: name + id + tags)
    //   @B #added: yo                 (partial declaration: id + tags)
    //   bob: hey                      (name reference)
    //   narrator text                 (no speaker)
    private static ScriptDocument SpeakerSample() =>
        new(
        [
            new Line(
                SpeakerDeclaration("alice", "A", CustomTag("mood")),
                [Text("hi "), CustomTag("aside")],
                SourceSpanFactory.Span()),
            new Line(
                new PartialSpeakerDeclaration("B", [CustomTag("added")], SourceSpanFactory.Span()),
                [Text("yo")],
                SourceSpanFactory.Span()),
            new Line(SpeakerNameReference("bob"), [Text("hey")], SourceSpanFactory.Span()),
            new Line(null, [Text("narrator text")], SourceSpanFactory.Span()),
        ]);

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
