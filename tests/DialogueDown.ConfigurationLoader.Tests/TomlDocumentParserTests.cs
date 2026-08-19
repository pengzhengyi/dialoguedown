using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Toml;
namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlDocumentParserTests
{
    private const string SourceName = "dialogue.toml";

    [Fact]
    public void Parse_ValidToml_ReturnsDocument()
    {
        var document = new TomlDocumentParser(SourceName).Parse("""
            [[speakers]]
            name = "Alice"
            """);

        Assert.False(document.HasErrors);
    }

    [Fact]
    public void Parse_MalformedToml_ThrowsWithLocatedError()
    {
        // The value is missing after the '=', a TOML syntax error on line 3.
        var parser = new TomlDocumentParser(SourceName);

        var exception = Assert.Throws<DialogueConfigurationException>(() => parser.Parse("""
            [[speakers]]
            name = "Alice"
            broken =
            """));

        Assert.Equal(SourceName, exception.Location.Source);
        Assert.Equal(3, exception.Location.Line);
        Assert.NotEqual(string.Empty, exception.Message);
    }
}
