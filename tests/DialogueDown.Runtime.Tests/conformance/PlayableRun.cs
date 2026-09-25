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

    /// <summary>Whether this build can play a node of this kind at all.</summary>
    /// <remarks>
    /// A second statement of what the runner knows, which is why a test holds the two to each
    /// other: were they to disagree, a case would be reported as untaught when it plays, or as a
    /// divergence when nobody had taught it.
    /// </remarks>
    /// <param name="node">The node to ask about.</param>
    /// <returns><see langword="true"/> when arriving at such a node is something this build does.</returns>
    internal static bool IsPlayable(Node node) =>
        node is LineNode or EndNode or ControlNode or BranchNode;

    // Asked before a step is taken, so a construct nobody has taught the runner is reported as that.
    private static bool TryFindUnplayable(
        PlayContext context, [NotNullWhen(true)] out string? construct)
    {
        construct = context.Playbook.Nodes
            .FirstOrDefault(node => !IsPlayable(node))
            ?.GetType().Name;

        return construct is not null;
    }

}
