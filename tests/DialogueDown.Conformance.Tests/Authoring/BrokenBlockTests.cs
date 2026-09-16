using DialogueDown.Conformance.Authoring;

namespace DialogueDown.Conformance;

public sealed class BrokenBlockTests
{
    [Fact]
    public void Parse_ABlockAndAScript_ReturnsTheNoteAndTheScript()
    {
        var source = """
            <!-- broken: the else is not last

                 > `else`
            -->
            > `if` `Rich?`
            >
            > Alice: Welcome.
            """;

        var block = BrokenBlock.Parse(source);

        Assert.Equal("the else is not last", block.Note);
        Assert.Equal("> `if` `Rich?`\n>\n> Alice: Welcome.", block.Script);
    }

    [Fact]
    public void Parse_ASourceThatDoesNotOpenWithTheMarker_IsRefused()
    {
        var source = """
            # The Inn

            Alice: Hello.
            """;

        Assert.Throws<MalformedBrokenBlockException>(() => BrokenBlock.Parse(source));
    }

    [Fact]
    public void Parse_ABlockWithNoCloser_IsRefused()
    {
        var source = """
            <!-- broken: the edit

                 evidence
            """;

        Assert.Throws<MalformedBrokenBlockException>(() => BrokenBlock.Parse(source));
    }

    [Fact]
    public void Parse_ABlockHoldingANestedOpener_IsRefused()
    {
        var source = """
            <!-- broken: the edit

                 <!-- nested -->
            -->
            > `if` `Rich?`
            """;

        Assert.Throws<MalformedBrokenBlockException>(() => BrokenBlock.Parse(source));
    }

    [Fact]
    public void Parse_ABlockWithNoBlankLineAfterTheNote_IsRefused()
    {
        var source = """
            <!-- broken: the edit
                 evidence
            -->
            > `if` `Rich?`
            """;

        Assert.Throws<MalformedBrokenBlockException>(() => BrokenBlock.Parse(source));
    }

    [Fact]
    public void Parse_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => BrokenBlock.Parse(null!));
    }
}
