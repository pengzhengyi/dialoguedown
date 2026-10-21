using DialogueDown.ConfigurationLoader.Errors;
using DialogueDown.ConfigurationLoader.Toml;
using static DialogueDown.ConfigurationLoader.Tests.Support.TomlConfigReading;

namespace DialogueDown.ConfigurationLoader.Tests;

public sealed class ConfigurationTableTests
{
    [Fact]
    public void Find_ASetKey_ReturnsItsEntry()
    {
        var root = TomlDocumentReader.Read("""
            mode = "best-effort"
            """, SourceName);

        var entry = root.Find("mode");

        Assert.Equal("mode", entry?.Key);
    }

    [Fact]
    public void Find_AnUnsetKey_ReturnsNull()
    {
        var root = TomlDocumentReader.Read("""
            mode = "best-effort"
            """, SourceName);

        Assert.Null(root.Find("speakers"));
    }

    [Fact]
    public void RejectUnknownKeys_OnlyKnownKeys_Passes()
    {
        var root = TomlDocumentReader.Read("""
            mode = "best-effort"
            """, SourceName);

        root.RejectUnknownKeys(["mode", "speakers"], "setting");
    }

    [Fact]
    public void RejectUnknownKeys_TwoUnknownKeys_RejectsTheFirstAtItsKey()
    {
        var root = TomlDocumentReader.Read("""
            mode = "best-effort"
            title = "My game"
            author = "Me"
            """, SourceName);

        var error = Assert.Throws<DialogueConfigurationException>(
            () => root.RejectUnknownKeys(["mode", "speakers"], "setting"));

        Assert.Equal("Unknown setting 'title'. Use 'mode', 'speakers'.", error.Message);
        Assert.Equal(new ConfigurationSourceLocation(SourceName, 2, 1), error.Location);
    }
}
