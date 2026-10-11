using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Speech;
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

    [Fact]
    public void From_AMenusOptions_OffersEveryOneInTheOrderItsNodeListsThem() =>
        Assert.Equal(
            ["Go east", "Go west", "Wait here"],
            OfferedOptions.From(ThreeWays(), available: [0, 1, 2]).Select(offered => SpeechText.Of(offered.Label)));

    [Fact]
    public void From_AMenusOptions_OffersOnlyTheOnesAtTheAvailablePositionsAsAvailable() =>
        Assert.Equal(
            [true, false, true],
            OfferedOptions.From(ThreeWays(), available: [0, 2]).Select(offered => offered.Available));

    /// <summary>The options of a menu offering three ways.</summary>
    /// <remarks>
    /// <code>
    /// - Go east
    /// - Go west
    /// - Wait here
    /// </code>
    /// </remarks>
    /// <returns>The options, in the order written.</returns>
    private static ImmutableArray<OptionEdge> ThreeWays() =>
        [Option("Go east", target: 1), Option("Go west", target: 2), Option("Wait here", target: 3)];

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
