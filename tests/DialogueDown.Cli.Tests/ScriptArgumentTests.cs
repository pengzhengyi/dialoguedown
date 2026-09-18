using DialogueDown.Cli.Commands;
using DialogueDown.TestSupport;

namespace DialogueDown.Cli.Tests;

public sealed class ScriptArgumentTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_Errors(string script)
    {
        var result = ScriptArgument.Validate(script);

        Assert.False(result.Successful);
    }

    [Fact]
    public void Validate_WrongExtension_Errors()
    {
        var result = ScriptArgument.Validate("notes.txt");

        Assert.False(result.Successful);
        Assert.Contains(ScriptArgument.Extension, result.Message!, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_MissingFile_Errors()
    {
        var result = ScriptArgument.Validate("does-not-exist.dialogue.md");

        Assert.False(result.Successful);
        Assert.Contains("not found", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ExistingScript_Succeeds()
    {
        using var script = new TempScript("# Scene");

        var result = ScriptArgument.Validate(script.Path);

        Assert.True(result.Successful);
    }
}
