using DialogueDown.ConfigurationLoader.Document;
using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Toml;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfigurationEntryTests
{
    [Fact]
    public void RequireString_AString_ReturnsIt()
    {
        var entry = OnlyEntry("""
            name = "Alice"
            """);

        Assert.Equal("Alice", entry.RequireString());
    }

    [Theory]
    [InlineData("name = 42")]
    [InlineData("name = true")]
    [InlineData("name = 1979-05-27")]
    [InlineData("name = [\"Alice\"]")]
    [InlineData("name = { first = \"Alice\" }")]
    public void RequireString_AnythingElse_IsRejectedAtTheValue(string toml)
    {
        var entry = OnlyEntry(toml);

        var error = Assert.Throws<DialogueConfigurationException>(entry.RequireString);

        Assert.Equal("'name' must be a string.", error.Message);
        Assert.Equal(new ConfigurationSourceLocation(SourceName, 1, 8), error.Location);
    }

    private static ConfigurationEntry OnlyEntry(string toml) =>
        Assert.Single(TomlDocumentReader.Read(toml, SourceName).Entries);
}
