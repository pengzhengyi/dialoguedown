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
/// A menu is offered without reading the world, so a menu whose options depend on the world is
/// refused rather than offered with an option's condition unasked or a label's query unfilled.
/// </para>
/// </remarks>
internal static class Choosing
{
    /// <summary>Offers a menu's options, and waits for the player to choose.</summary>
    /// <param name="position">The menu's position in the playbook.</param>
    /// <param name="choice">The menu itself.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    public static StepResult Offer(int position, ChoiceNode choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        return AsksTheWorld(choice) ? RefuseAMenuThatAsksTheWorld(position) : OfferEveryOption(position, choice);
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

    private static bool AsksTheWorld(ChoiceNode choice) =>
        choice.Options().Any(option => option.Condition is not null || SpeechTemplate.HasKeys(option.Label));

    // Every option is offered, in the order the node lists them. Nothing guards an option here, so
    // every one of them can be taken.
    private static StepResult OfferEveryOption(int position, ChoiceNode choice)
    {
        var options = choice.Options();

        return new(
            new PlayState(new AwaitingChoice(position, EveryOptionAvailable(options))),
            [new Offer(choice.Ordered, [.. options.Select(option => OfferedOptions.From(option, available: true))])]);
    }

    private static ImmutableArray<int> EveryOptionAvailable(ImmutableArray<OptionEdge> options) =>
        [.. Enumerable.Range(0, options.Length)];

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

    private static StepResult RefuseAMenuThatAsksTheWorld(int position) =>
        StepResults.Refuse(
            position,
            RefusalReason.UnplayableNode,
            "This build cannot yet offer a menu whose options ask the world anything.");
}
