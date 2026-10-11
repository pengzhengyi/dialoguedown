using DialogueDown.Playbook.Edges;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// How an option is offered: its label, and whether the player can take it.
/// </summary>
public sealed class OfferedOptionsTests
{
    [Fact]
    public void From_ALabelOfPlainWords_IsOfferedAsWritten() =>
        Assert.Equal([Text("Go east")], OfferedOptions.From(Option("Go east"), available: true).Label);

    [Fact]
    public void From_ALabelHoldingACommand_KeepsTheWordsEitherSideAndLeavesOutTheCommand() =>
        Assert.Equal(
            [Text("Say goodbye "), Text(" and go")],
            OfferedOptions.From(AnOptionWithACommandInItsLabel(), available: true).Label);

    [Fact]
    public void From_ALabelThatIsOnlyACommand_IsEmpty() =>
        Assert.Empty(OfferedOptions.From(OptionOf([CustomCommand("Wave")]), available: true).Label);

    [Fact]
    public void From_AnOptionThePlayerCanTake_IsOfferedAvailable() =>
        Assert.True(OfferedOptions.From(Option("Go east"), available: true).Available);

    [Fact]
    public void From_AnOptionThePlayerCannotTake_IsOfferedUnavailable() =>
        Assert.False(OfferedOptions.From(Option("Go east"), available: false).Available);

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
        OptionOf([Text("Say goodbye "), CustomCommand("Wave"), Text(" and go")]);
}
