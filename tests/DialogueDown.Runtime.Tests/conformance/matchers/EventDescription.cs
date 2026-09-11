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
            Ended => "ended",
            Refused refused => $"refused: {refused.Because}",
            _ => happened.GetType().Name,
        };
}
