using DialogueDown.ConfigurationLoader.Toml;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlLocationTests
{
    [Fact]
    public void From_ConvertsZeroBasedSpanToOneBasedLocation()
    {
        var start = new TextPosition(offset: 20, line: 2, column: 6);
        var end = new TextPosition(offset: 25, line: 2, column: 11);
        var span = new SourceSpan("dialogue.toml", start, end);

        var location = TomlLocation.From(span);

        Assert.Equal(new ConfigurationSourceLocation("dialogue.toml", 3, 7), location);
    }
}
