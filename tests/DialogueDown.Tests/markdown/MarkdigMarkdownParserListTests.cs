using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserListTests : MarkdigMarkdownParserTestBase
{
    [Theory]
    [InlineData("- item")]
    [InlineData("+ item")]
    [InlineData("* item")]
    public void Parse_UnorderedListMarkers_ProduceUnorderedList(string source)
    {
        var list = AssertSingleBlock<ListBlock>(Parse(source));

        Assert.False(list.IsOrdered);
        Assert.Single(list.Items);
        AssertItemText(list, 0, "item");
    }

    [Theory]
    [InlineData("1. item")]
    [InlineData("1) item")]
    public void Parse_OrderedListMarkers_ProduceOrderedList(string source)
    {
        var list = AssertSingleBlock<ListBlock>(Parse(source));

        Assert.True(list.IsOrdered);
        Assert.Single(list.Items);
        AssertItemText(list, 0, "item");
    }

    [Fact]
    public void Parse_ListItemWithExtraWhitespace_TrimsToContent()
    {
        // Extra spaces after the marker are indentation, not part of the content.
        var list = AssertSingleBlock<ListBlock>(Parse("-    item"));

        Assert.Single(list.Items);
        AssertItemText(list, 0, "item");
    }

    [Fact]
    public void Parse_ListWithMultipleItems_KeepsTextualOrder()
    {
        var list = AssertSingleBlock<ListBlock>(Parse("- one\n- two"));

        Assert.Equal(2, list.Items.Count);
        AssertItemText(list, 0, "one");
        AssertItemText(list, 1, "two");
    }

    [Theory]
    [InlineData("- one\n- two")]
    [InlineData("- one\r\n- two")]
    public void Parse_ListWithDifferentLineEndings_ProducesSameItems(string source)
    {
        var list = AssertSingleBlock<ListBlock>(Parse(source));

        Assert.Equal(2, list.Items.Count);
        AssertItemText(list, 0, "one");
        AssertItemText(list, 1, "two");
    }

    [Fact]
    public void Parse_NestedList_PreservesNestingAndSuccession()
    {
        // A choice with a follow-up line and a nested choice underneath it.
        var list = AssertSingleBlock<ListBlock>(Parse("- Bob: Really?\n    - Alice: Yes."));

        var item = Assert.Single(list.Items);
        Assert.Equal(2, item.Blocks.Count);

        var paragraph = Assert.IsType<Paragraph>(item.Blocks[0]);
        AssertSingleText(paragraph.Inlines, "Bob: Really?");

        var nested = Assert.IsType<ListBlock>(item.Blocks[1]);
        AssertItemText(nested, 0, "Alice: Yes.");
    }
}
