using DialogueDown.TestSupport;
using DialogueDown.Visualization.Live.Files;
namespace DialogueDown.Visualization.Live.Tests;

public sealed class DocumentValidationTests
{
    [Fact]
    public void Validate_ExistingDialogueMd_ReturnsNull()
    {
        using var script = new TempScript("""
            # Scene

            Alice: Hi.
            """);

        Assert.Null(DocumentValidation.Validate(script.Path));
    }

    [Fact]
    public void Validate_WrongExtension_ReturnsError()
    {
        var result = DocumentValidation.Validate("/tmp/notes.txt");

        Assert.Equal("Expected a DialogueDown script ending in '.dialogue.md': /tmp/notes.txt", result);
    }

    [Fact]
    public void Validate_MissingFile_ReturnsError()
    {
        var result = DocumentValidation.Validate("/tmp/does-not-exist.dialogue.md");

        Assert.Equal("File not found: /tmp/does-not-exist.dialogue.md", result);
    }
}
