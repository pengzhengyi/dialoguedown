using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Says what a run did, in words a divergence message can quote.</summary>
internal static class EventDescription
{
    /// <summary>Describes an event, in a phrase that follows "the run".</summary>
    /// <param name="happened">What the runner said.</param>
    /// <returns>The phrase.</returns>
    public static string Describe(this Event happened) =>
        happened switch
        {
            Said spoke => $"heard {SpeakerNames.Of(spoke.Speaker)} speak",
            Continued => "heard the line go on",
            Ended => "ended",
            Refused refused => $"refused: {refused.Explanation}",
            Resolve resolve => $"asked the world about {string.Join(", ", resolve.Keys)}",
            Offer offer => $"offered a menu: {Labels(offer)}",
            _ => happened.GetType().Name,
        };

    // Each label is quoted, so a label holding a comma still reads as one option.
    private static string Labels(Offer offer) =>
        string.Join(", ", offer.Options.Select(option => $"\"{SpeechText.Of(option.Label)}\""));
}
