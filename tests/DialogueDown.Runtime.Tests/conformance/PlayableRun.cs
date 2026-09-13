using System.Diagnostics.CodeAnalysis;
using DialogueDown.Conformance;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>Runs one corpus case: reads its playbook, then holds its session against a runner.</summary>
internal static class PlayableRun
{
    /// <summary>Runs a case.</summary>
    /// <param name="playable">The case to run.</param>
    /// <returns>What became of it.</returns>
    public static SessionOutcome Of(PlayableCase playable)
    {
        var context = PlayContext.Of(PlaybookReader.Default.Read(playable.Playbook));

        if (TryFindUnplayable(context, out var construct))
        {
            return SessionOutcome.NotYetRunnable($"nothing plays a {construct} yet");
        }

        return SessionMatcher.Match(new SessionOperator(context), playable.Fixture.Session);
    }

    // Asked before a step is taken, so a construct nobody has taught the runner is reported as that.
    private static bool TryFindUnplayable(
        PlayContext context, [NotNullWhen(true)] out string? construct)
    {
        construct = context.Playbook.Nodes
            .FirstOrDefault(node => node is not (LineNode or EndNode))
            ?.GetType().Name;

        return construct is not null;
    }
}
