using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a menu does: offering the player its options, and waiting for them to choose.
/// </summary>
/// <remarks>
/// A menu is offered without reading the world, so a menu whose options depend on the world is
/// refused rather than offered with an option's condition unasked or a label's query unfilled.
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

    private static bool AsksTheWorld(ChoiceNode choice) =>
        Options(choice).Any(option => option.Condition is not null || SpeechTemplate.HasKeys(option.Label));

    // Every option is offered, in the order the node lists them. Nothing guards an option here, so
    // every one of them can be taken.
    private static StepResult OfferEveryOption(int position, ChoiceNode choice) =>
        new(
            new PlayState(new AwaitingChoice(position)),
            [new Offer(choice.Ordered, [.. Options(choice).Select(Offered)])]);

    private static OfferedOption Offered(OptionEdge option) =>
        new([.. SpeechTemplate.Segments(option.Label).SelectMany(segment => segment.Words)], Available: true);

    // A choice node's fall-through is not an option, so it is never offered.
    private static IEnumerable<OptionEdge> Options(ChoiceNode choice) => choice.Out.OfType<OptionEdge>();

    private static StepResult RefuseAMenuThatAsksTheWorld(int position) =>
        StepResults.Refuse(
            position,
            RefusalReason.UnplayableNode,
            "This build cannot yet offer a menu whose options ask the world anything.");
}
