using DialogueDown.Cli.Commands;
using DialogueDown.TestSupport;
using static DialogueDown.Cli.Tests.Support.ValidationAssert;

namespace DialogueDown.Cli.Tests;

public sealed class ConfigArgumentTests
{
    [Fact]
    public void Validate_Null_Succeeds()
    {
        AssertAccepted(ConfigArgument.Validate(null));
    }

    [Fact]
    public void Validate_Whitespace_Fails()
    {
        var result = ConfigArgument.Validate("   ");

        AssertRejected(result, "requires a path");
    }

    [Fact]
    public void Validate_MissingFile_Fails()
    {
        var result = ConfigArgument.Validate("no-such.toml");

        AssertRejected(result, "not found");
    }

    [Fact]
    public void Validate_ExistingFile_Succeeds()
    {
        using var tree = new TempTree();
        var configPath = tree.File("dialogue.toml", "");

        AssertAccepted(ConfigArgument.Validate(configPath));
    }
}
