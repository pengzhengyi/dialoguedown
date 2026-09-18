using DialogueDown.Conformance;
using DialogueDown.Playbook;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Runs one corpus case: reads its playbook, screens it, then holds its session against a runner.</summary>
internal static class PlayableRun
{
    /// <summary>Runs a case.</summary>
    /// <param name="playable">The case to run.</param>
    /// <returns>What became of it.</returns>
    public static SessionOutcome Of(PlayableCase playable)
    {
        var session = playable.Fixture.Session;
        var context = PlayContext.Of(PlaybookReader.Default.Read(playable.Playbook));
        var untaught = Playability.WhyNotPlayable(context)
            .Concat(Playability.WhyNotPlayable(session))
            .ToList();

        return untaught.Count > 0
            ? SessionOutcome.NotYetPlayable(untaught)
            : SessionMatcher.Match(new SessionOperator(context), session);
    }
}
