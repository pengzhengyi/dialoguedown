using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Toml;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class TomlErrorsTests
{
    [Fact]
    public void At_NullMessage_Throws()
    {
        DocumentSyntax document = TomlConfigReading.Parse("mode = 42");

        Assert.Throws<ArgumentNullException>(
            () => TomlErrors.At(null!, document.KeyValues.Single().Value!));
    }

    [Fact]
    public void At_NullNode_Throws() =>
        Assert.Throws<ArgumentNullException>(() => TomlErrors.At("Invalid value.", null!));

    [Fact]
    public void At_MessageAndNode_ReturnsALocatedConfigurationError()
    {
        DocumentSyntax document = TomlConfigReading.Parse("""
            title = "Example"
            mode = 42
            """);
        SyntaxNode value = document.KeyValues.ElementAt(1).Value!;

        DialogueConfigurationException exception = TomlErrors.At("'mode' must be a string.", value);

        Assert.Equal("'mode' must be a string.", exception.Message);
        Assert.Equal(new ConfigurationSourceLocation("dialogue.toml", 2, 8), exception.Location);
    }
}
