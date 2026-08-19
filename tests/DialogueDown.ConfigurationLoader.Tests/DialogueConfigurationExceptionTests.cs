using DialogueDown.ConfigurationLoader.Errors;
namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class DialogueConfigurationExceptionTests
{
    [Fact]
    public void Constructor_SetsMessageAndLocation()
    {
        var location = new ConfigurationSourceLocation("dialogue.toml", 2, 4);

        var exception = new DialogueConfigurationException("bad config", location);

        Assert.Equal("bad config", exception.Message);
        Assert.Equal(location, exception.Location);
    }
}
