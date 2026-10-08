using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;
using static DialogueDown.Runtime.Tests.StepAssert;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// What a menu offers the player, asked of the menu alone.
/// </summary>
public sealed class ChoosingTests
{
    [Fact]
    public void Offer_AMenu_OffersEveryOptionInTheOrderItsNodeListsThem() =>
        AssertEvents(Offering(TwoWays()), "offer 'Go east', 'Go west'");

    [Fact]
    public void Offer_AMenu_WaitsForThePlayerToChoose() => AssertAwaitingChoice(Offering(TwoWays()), 0);

    [Fact]
    public void Offer_ANumberedMenu_SaysItsOrderIsTheWritersToKeep() =>
        AssertEvents(Offering(ANumberedMenu()), "offer ordered 'Open the door', 'Wait here'");

    [Fact]
    public void Offer_AMenuWithAFallThrough_OffersOnlyItsOptions() =>
        AssertEvents(Offering(AMenuWithAFallThrough()), "offer 'Go east'");

    [Fact]
    public void Offer_AMenuWhoseLabelHoldsACommand_OffersTheWordsAroundItAndPerformsNothing() =>
        // The command is performed when the option's arm plays, not when the menu is offered.
        AssertEvents(Offering(AMenuWithACommandInALabel()), "offer 'Say goodbye  and go'");

    [Fact]
    public void Offer_AMenuWhoseLabelIsOnlyACommand_OffersAnEmptyLabel() =>
        AssertEvents(Offering(AMenuWhoseLabelIsOnlyACommand()), "offer ''");

    [Fact]
    public void Offer_AMenuWithAConditionalOption_IsRefusedRatherThanOfferedUnasked() =>
        AssertRefused(Offering(AMenuWithAConditionalOption()), RefusalReason.UnplayableNode, "ask the world");

    [Fact]
    public void Offer_AMenuWhoseLabelHoldsAQuery_IsRefusedRatherThanOfferedUnfilled() =>
        AssertRefused(Offering(AMenuWithAQueryInALabel()), RefusalReason.UnplayableNode, "ask the world");

    [Fact]
    public void Offer_NoMenu_IsRejected() => Assert.Throws<ArgumentNullException>(() => Choosing.Offer(0, null!));

    /// <summary>Offers a menu standing at the start of a playbook.</summary>
    /// <param name="menu">The menu under test.</param>
    /// <returns>What offering it produced.</returns>
    private static StepResult Offering(ChoiceNode menu) => Choosing.Offer(0, menu);

    /// <summary>A menu of two options.</summary>
    /// <remarks>
    /// <code>
    /// - Go east
    /// - Go west
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode TwoWays() => Menu(0, Option(1, "Go east"), Option(2, "Go west"));

    /// <summary>A numbered menu of two options.</summary>
    /// <remarks>
    /// <code>
    /// 1. Open the door
    /// 2. Wait here
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode ANumberedMenu() =>
        NumberedMenu(0, Option(1, "Open the door"), Option(2, "Wait here"));

    /// <summary>A menu of one option, with a fall-through beside it.</summary>
    /// <remarks>
    /// <code>
    /// - Go east
    /// </code>
    /// The compiler writes a fall-through when every option has a condition, and a reader accepts
    /// one beside an option that has none.
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode AMenuWithAFallThrough() => Menu(0, Option(1, "Go east"), new SuccessionEdge(2));

    /// <summary>A menu whose only option has a command in the middle of its label.</summary>
    /// <remarks>
    /// <code>
    /// - Say goodbye `Wave()` and go
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode AMenuWithACommandInALabel() =>
        Menu(0, OptionSaying(1, new TextFragment("Say goodbye "), Command("Wave"), new TextFragment(" and go")));

    /// <summary>A menu whose only option's label is a command.</summary>
    /// <remarks>
    /// <code>
    /// - `Wave()`
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode AMenuWhoseLabelIsOnlyACommand() => Menu(0, OptionSaying(1, Command("Wave")));

    /// <summary>A menu whose first option only the world can allow.</summary>
    /// <remarks>
    /// <code>
    /// - `Alice.HasKey?` Open the door
    /// - Wait here
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode AMenuWithAConditionalOption() =>
        Menu(0, Option(1, "Open the door", "Alice.HasKey"), Option(2, "Wait here"));

    /// <summary>A menu whose only option's label asks the world for words.</summary>
    /// <remarks>
    /// <code>
    /// - Call `"playerName"` over
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    private static ChoiceNode AMenuWithAQueryInALabel() =>
        Menu(0, OptionSaying(1, new TextFragment("Call "), new QueryFragment("playerName"), new TextFragment(" over")));

    private static CustomCommandFragment Command(string name) => new(name, []);
}
