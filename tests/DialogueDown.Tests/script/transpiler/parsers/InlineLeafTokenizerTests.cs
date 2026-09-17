using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsers;
using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.InlineLeafAssert;

namespace DialogueDown.Tests.Script.Transpiler.Parsers;

public sealed class InlineLeafTokenizerTests
{
    [Fact]
    public void Tokenize_PlainText_IsOneTextLeafSpanningItAll()
    {
        var leaves = Tokenize("Hello there");

        var leaf = Assert.Single(leaves);
        AssertTextLeaf(leaf, "Hello there");
        AssertRange(leaf, start: 0, length: 11);
    }

    [Fact]
    public void Tokenize_AnchorsRangesAtTheInputPosition()
    {
        var leaves = InlineLeafTokenizer.Tokenize(
            ParseInputFactory.Input("hi", position: 5), allowJumps: true);

        Assert.Equal(5, Assert.Single(leaves).Range.Start);
    }

    [Fact]
    public void Tokenize_JumpArrow_SplitsTextAroundAJumpLeaf()
    {
        var leaves = Tokenize("go => there");

        Assert.Collection(
            leaves,
            leaf => AssertTextLeaf(leaf, "go "),
            leaf =>
            {
                AssertJumpLeaf(leaf);
                AssertRange(leaf, start: 3, length: 2);
            },
            leaf => AssertTextLeaf(leaf, " there"));
    }

    [Fact]
    public void Tokenize_WhenJumpsDisallowed_TheArrowStaysText()
    {
        var leaves = InlineLeafTokenizer.Tokenize(
            ParseInputFactory.Input("go => there"), allowJumps: false);

        AssertTextLeaf(Assert.Single(leaves), "go => there");
    }

    [Fact]
    public void Tokenize_StrayHashOrEquals_StaysText()
    {
        // A '#' that starts no tag and an '=' that starts no jump are plain text.
        var leaves = Tokenize("a # b = c");

        AssertTextLeaf(Assert.Single(leaves), "a # b = c");
    }

    [Fact]
    public void Tokenize_Tag_SplitsTextAroundATagLeaf()
    {
        var leaves = Tokenize("mood #happy");

        Assert.Collection(
            leaves,
            leaf => AssertTextLeaf(leaf, "mood "),
            leaf =>
            {
                var tag = AssertTagLeaf(leaf);
                Assert.Equal("happy", tag.Name);
                Assert.False(tag.IsReserved);
                AssertRange(leaf, start: 5, length: 6);
            });
    }

    [Fact]
    public void Tokenize_ReservedAndGroupTags_AreRecognized()
    {
        var leaves = Tokenize("##npc #mood=happy");

        Assert.Collection(
            leaves,
            leaf => Assert.True(AssertTagLeaf(leaf).IsReserved),
            leaf => AssertTextLeaf(leaf, " "),
            leaf =>
            {
                var tag = AssertTagLeaf(leaf);
                Assert.Equal("mood", tag.Name);
                Assert.Equal("happy", tag.Value);
            });
    }

    [Fact]
    public void Tokenize_TagsAndJumpsMix_InOneString()
    {
        var leaves = Tokenize("see #here => go");

        Assert.Collection(
            leaves,
            leaf => AssertTextLeaf(leaf, "see "),
            leaf => Assert.Equal("here", AssertTagLeaf(leaf).Name),
            leaf => AssertTextLeaf(leaf, " "),
            AssertJumpLeaf,
            leaf => AssertTextLeaf(leaf, " go"));
    }

    [Fact]
    public void Tokenize_EscapedLeadingCharacter_BeforeATag_LeavesTheWholeTagAsText()
    {
        var leaves = TokenizeEscaped("#happy");

        var leaf = Assert.Single(leaves);
        AssertTextLeaf(leaf, "#happy");
        AssertRange(leaf, start: 0, length: 6);
    }

    [Fact]
    public void Tokenize_EscapedLeadingCharacter_BeforeAJump_LeavesTheArrowAsText()
    {
        var leaves = TokenizeEscaped("=> go");

        AssertTextLeaf(Assert.Single(leaves), "=> go");
    }

    [Fact]
    public void Tokenize_EscapedLeadingCharacter_ThatStartsNoSigil_IsLiteralAlone()
    {
        // "=" begins no sigil, so only it is escaped; the following tag is real.
        var leaves = TokenizeEscaped("=#happy");

        Assert.Collection(
            leaves,
            leaf => AssertTextLeaf(leaf, "="),
            leaf => Assert.Equal("happy", AssertTagLeaf(leaf).Name));
    }

    private static IReadOnlyList<Spanned<InlineLeaf>> TokenizeEscaped(string text) =>
        InlineLeafTokenizer.Tokenize(
            ParseInputFactory.Input(text), allowJumps: true, escapedFirstCharacter: true);

    private static IReadOnlyList<Spanned<InlineLeaf>> Tokenize(string text) =>
        InlineLeafTokenizer.Tokenize(ParseInputFactory.Input(text), allowJumps: true);
}
