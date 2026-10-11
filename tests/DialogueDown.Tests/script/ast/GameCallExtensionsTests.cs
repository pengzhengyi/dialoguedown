using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class GameCallExtensionsTests
{
    [Fact]
    public void Canonical_AQuery_IsTheQuotedKey() =>
        Assert.Equal("\"PlaceName\"", Query("PlaceName").Canonical());

    [Fact]
    public void Canonical_ADefaultCommand_IsTheQuotedActionInParentheses() =>
        Assert.Equal("(\"wave\")", DefaultCommand("wave").Canonical());

    [Fact]
    public void Canonical_ANamedCommand_ListsItsQuotedArguments() =>
        Assert.Equal("GiveQuest(\"EmberCrown\", \"3\")", CustomCommand("GiveQuest", "EmberCrown", "3").Canonical());

    [Fact]
    public void Canonical_ANamedCommandWithNoArguments_HasEmptyParentheses() =>
        Assert.Equal("SlamDoor()", CustomCommand("SlamDoor").Canonical());
}
