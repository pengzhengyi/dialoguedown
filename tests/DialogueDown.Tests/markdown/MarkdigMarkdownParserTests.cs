using DialogueDown.Markdown;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Constructor_NullPolicy_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new MarkdigMarkdownParser(null!));

    [Fact]
    public void Parse_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    public void Parse_EmptyOrWhitespace_ReturnsEmptyDocument(string source)
    {
        var document = Parse(source);

        Assert.Empty(document.Blocks);
    }
}
