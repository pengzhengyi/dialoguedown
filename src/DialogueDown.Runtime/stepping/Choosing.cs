using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a menu does: offering the player its options, and taking the one they choose.
/// </summary>
/// <remarks>
/// The offer and the choice are kept together because both read the same options: a choice is
/// counted against the options in the order the offer listed them.
/// <para>
/// A menu's options' conditions are read on arrival, so an option the world withholds is offered
/// as unavailable rather than hidden. A label's query is not filled yet, so a menu whose labels ask
/// the world is refused rather than offered with a query unfilled; so is a menu with no option the
/// player can take, until it can be walked past.
/// </para>
/// </remarks>
internal static class Choosing
{
    /// <summary>Offers a menu's options, and waits for the player to choose.</summary>
    /// <param name="position">The menu's position in the playbook.</param>
    /// <param name="choice">The menu itself.</param>
    /// <param name="supply">
    /// What the world said about the options' conditions, or <see langword="null"/> when nothing
    /// was asked.
    /// </param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult Offer(int position, ChoiceNode choice, Supply? supply = null)
    {
        ArgumentNullException.ThrowIfNull(choice);

        if (ALabelAsksTheWorld(choice))
        {
            return RefuseAMenuWhoseLabelAsksTheWorld(position);
        }

        var options = choice.Options();
        var available = AvailableOptions(options, supply);

        return available.IsEmpty
            ? RefuseAMenuWithNothingAvailable(position)
            : OfferOptions(position, choice.Ordered, options, available);
    }

    /// <summary>Takes the option the player chose, and arrives at the node it leads to.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="waiting">The menu waiting for the player.</param>
    /// <param name="choose">The player's choice, by its position in the offer.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    /// <remarks>
    /// A menu asks the world nothing as it is left, so the chosen option's node is arrived at
    /// directly. A choice the menu cannot take is refused, and the menu stays open for another.
    /// </remarks>
    public static StepResult Chosen(PlayContext context, AwaitingChoice waiting, Choose choose)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(waiting);
        ArgumentNullException.ThrowIfNull(choose);

        return context.NodeAt(waiting.Node) switch
        {
            ChoiceNode choice => Take(context, waiting, choice.Options(), choose.Index),
            var notAMenu => RefuseChoose(waiting, notAMenu),
        };
    }

    private static bool ALabelAsksTheWorld(ChoiceNode choice) =>
        choice.Options().Any(option => SpeechTemplate.HasQueries(option.Label));

    // An option nothing guards can always be taken, and a guarded one only when the world allows
    // it. Without answers, only an option nothing guards can be taken.
    private static ImmutableArray<int> AvailableOptions(ImmutableArray<OptionEdge> options, Supply? supply) =>
        [.. Enumerable.Range(0, options.Length).Where(at => IsAvailable(options[at], supply))];

    private static bool IsAvailable(OptionEdge option, Supply? supply) =>
        option.Condition is null || (supply is not null && supply.Allows(option));

    // Every option is offered, in the order the node lists them, and the wait keeps the ones the
    // player can take.
    private static StepResult OfferOptions(
        int position, bool ordered, ImmutableArray<OptionEdge> options, ImmutableArray<int> available)
    {
        var offer = new Offer(ordered, OfferedOptions.From(options, available));
        var waiting = new AwaitingChoice(position, available);

        return new StepResult(new PlayState(waiting), [offer]);
    }

    // The node says how many options there are; the wait says which of them could be taken.
    private static StepResult Take(
        PlayContext context, AwaitingChoice waiting, ImmutableArray<OptionEdge> options, int index)
    {
        if (index < 0 || index >= options.Length)
        {
            return RefuseNoSuchOption(waiting, options.Length, index);
        }

        return waiting.Available.Contains(index)
            ? Arrival.At(context, options[index].Target)
            : RefuseUnavailableOption(waiting, index);
    }

    private static StepResult RefuseNoSuchOption(AwaitingChoice waiting, int offered, int index) =>
        new(
            new PlayState(waiting),
            [
                new Refused(
                    RefusalReason.NoSuchOption,
                    $"The menu at node {waiting.Node} has no option {index}; choose from 0 to {offered - 1}."),
            ]);

    private static StepResult RefuseUnavailableOption(AwaitingChoice waiting, int index) =>
        new(
            new PlayState(waiting),
            [
                new Refused(
                    RefusalReason.UnavailableOption,
                    $"The menu at node {waiting.Node} offered option {index} as one the player cannot take."),
            ]);

    // A wait at a menu can be restored against a playbook whose node there is something else.
    private static StepResult RefuseChoose(AwaitingChoice waiting, Node notAMenu) =>
        new(
            new PlayState(waiting),
            [
                new Refused(
                    RefusalReason.Misplaced,
                    $"Node {waiting.Node}, of kind {notAMenu.GetType().Name}, is not a menu, so there is "
                        + "no Choose to take there."),
            ]);

    private static StepResult RefuseAMenuWhoseLabelAsksTheWorld(int position) =>
        StepResults.Refuse(
            position,
            RefusalReason.UnplayableNode,
            "This build cannot yet offer a menu whose labels ask the world anything.");

    private static StepResult RefuseAMenuWithNothingAvailable(int position) =>
        StepResults.Refuse(
            position,
            RefusalReason.UnplayableNode,
            "This build cannot yet walk past a menu with no option the player can take.");
}
