using System.Collections.Immutable;
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
        var untaught = ReasonsNotYetPlayable(context, playable.Fixture.Session);

        return untaught.Length > 0
            ? SessionOutcome.Combine(untaught.Select(SessionOutcome.NotYetPlayable))
            : SessionMatcher.Match(new SessionOperator(context), playable.Fixture.Session);
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
        node is LineNode or EndNode or ControlNode;

    /// <summary>Whether this build can take a session entry at all.</summary>
    /// <remarks>
    /// An expectation is always checkable: a claim inside it that nobody has taught the harness is
    /// reported where it is read. A send needs a reader, or there is nothing to send it through.
    /// </remarks>
    /// <param name="entry">The entry to ask about.</param>
    /// <returns><see langword="true"/> when the harness can take this entry.</returns>
    internal static bool IsPlayable(SessionEntry entry) =>
        entry is not Send send || Commands.TryRead(send, out _);

    /// <summary>
    /// Everything the case needs that this build has not learned, each named once: a node kind it
    /// cannot play, and a send no reader owns.
    /// </summary>
    /// <remarks>
    /// Asked of the fixture rather than found while running it, so one run lists everything the
    /// build has yet to learn. Carrying on past an unsendable message would be wrong instead: the
    /// run stands still, so later entries would be judged against a state the fixture never
    /// described.
    /// </remarks>
    /// <param name="context">The playbook the case loads.</param>
    /// <param name="session">What the case sends and expects.</param>
    /// <returns>The reasons, in the order the playbook and the session name them.</returns>
    internal static ImmutableArray<string> ReasonsNotYetPlayable(
        PlayContext context, ImmutableArray<SessionEntry> session) =>
    [
        .. context.Playbook.Nodes
            .Where(node => !IsPlayable(node))
            .Select(node => node.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .Select(SessionReasons.NothingPlays),
        .. session
            .OfType<Send>()
            .Where(send => !IsPlayable(send))
            .Select(send => send.Message.ToJsonString())
            .Distinct(StringComparer.Ordinal)
            .Select(SessionReasons.NothingSends),
    ];
}
