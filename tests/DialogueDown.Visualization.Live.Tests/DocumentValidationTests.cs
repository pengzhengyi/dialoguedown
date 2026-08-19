using DialogueDown.Visualization.Live.Files;
namespace DialogueDown.Visualization.Live.Tests;

public sealed class DocumentValidationTests
{
    [Fact]
    public void Validate_ExistingDialogueMd_ReturnsNull()
    {
        using var doc = new Support.TempDocument();

        Assert.Null(DocumentValidation.Validate(doc.Path));
    }

    [Fact]
    public void Validate_WrongExtension_ReturnsError()
    {
        var result = DocumentValidation.Validate("/tmp/notes.txt");

        Assert.NotNull(result);
        Assert.Contains(".dialogue.md", result);
    }

    [Fact]
    public void Validate_MissingFile_ReturnsError()
    {
        var result = DocumentValidation.Validate("/tmp/does-not-exist.dialogue.md");

        Assert.NotNull(result);
        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
    }
}
