using DialogueDown.Cli.Commands;
using DialogueDown.TestSupport;
using static DialogueDown.Cli.Tests.Support.ValidationAssert;

namespace DialogueDown.Cli.Tests;

public sealed class ScriptArgumentTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_Errors(string script)
    {
        var result = ScriptArgument.Validate(script);

        AssertRejected(result, "A script path is required.");
    }

    [Fact]
    public void Validate_WrongExtension_Errors()
    {
        var result = ScriptArgument.Validate("notes.txt");

        AssertRejected(result, "Expected a DialogueDown script ending in '.dialogue.md': notes.txt");
    }

    [Fact]
    public void Validate_MissingFile_Errors()
    {
        var result = ScriptArgument.Validate("does-not-exist.dialogue.md");

        AssertRejected(result, "File not found: does-not-exist.dialogue.md");
    }

    [Fact]
    public void Validate_ExistingScript_Succeeds()
    {
        using var script = new TempScript("# Scene");

        var result = ScriptArgument.Validate(script.Path);

        AssertAccepted(result);
    }
}
