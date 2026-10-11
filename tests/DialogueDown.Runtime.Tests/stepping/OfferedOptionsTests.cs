using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// How an option is offered: its label, and whether the player can take it.
/// </summary>
public sealed class OfferedOptionsTests
{
    [Fact]
    public void From_ALabelOfPlainWords_IsOfferedAsWritten() =>
        Assert.Equal([new TextFragment("Go east")], OfferedOptions.From(Option(1, "Go east"), available: true).Label);

    [Fact]
    public void From_ALabelHoldingACommand_KeepsTheWordsEitherSideAndLeavesOutTheCommand() =>
        Assert.Equal(
            [new TextFragment("Say goodbye "), new TextFragment(" and go")],
            OfferedOptions.From(AnOptionWithACommandInItsLabel(), available: true).Label);

    [Fact]
    public void From_ALabelThatIsOnlyACommand_IsEmpty() =>
        Assert.Empty(OfferedOptions.From(OptionSaying(1, Wave()), available: true).Label);

    [Fact]
    public void From_AnOptionThePlayerCanTake_IsOfferedAvailable() =>
        Assert.True(OfferedOptions.From(Option(1, "Go east"), available: true).Available);

    [Fact]
    public void From_AnOptionThePlayerCannotTake_IsOfferedUnavailable() =>
        Assert.False(OfferedOptions.From(Option(1, "Go east"), available: false).Available);

    [Fact]
    public void From_NoOption_IsRejected() =>
        Assert.Throws<ArgumentNullException>(() => OfferedOptions.From(null!, available: true));

    /// <summary>An option with a command in the middle of its label.</summary>
    /// <remarks>
    /// <code>
    /// - Say goodbye `Wave()` and go
    /// </code>
    /// </remarks>
    /// <returns>The option.</returns>
    private static OptionEdge AnOptionWithACommandInItsLabel() =>
        OptionSaying(1, new TextFragment("Say goodbye "), Wave(), new TextFragment(" and go"));

    private static CustomCommandFragment Wave() => new("Wave", []);
}
