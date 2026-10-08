using System.Collections.Immutable;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Tests.Conformance.Matchers;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// What this build can play, in one place: the node kinds the runner plays, the sends a reader
/// takes, and why a playbook or a session is not there yet.
/// </summary>
/// <remarks>
/// A case is screened before it runs, so a construct the runner cannot play reads as not yet
/// playable rather than as a divergence. The screen restates what the runner can play, so a test
/// checks that the two agree.
/// <para>
/// A node is playable by its kind alone. A send is playable when a reader owns it, and an
/// expectation when the harness can check every claim in it.
/// </para>
/// </remarks>
internal static class Playability
{
    /// <summary>Whether this build can play a node of this kind at all.</summary>
    /// <param name="node">The node to ask about.</param>
    /// <returns><see langword="true"/> when arriving at such a node is something this build does.</returns>
    public static bool CanPlay(Node node) =>
        node is LineNode or EndNode or ControlNode or BranchNode or ChoiceNode;

    /// <summary>Whether this build can take a session entry at all.</summary>
    /// <param name="entry">The entry to ask about.</param>
    /// <returns><see langword="true"/> when the harness can take this entry.</returns>
    public static bool CanPlay(SessionEntry entry) => entry switch
    {
        Send send => Commands.TryRead(send, out _),
        Expect expect => !WhyNotPlayable(expect).Any(),
        _ => true,
    };

    /// <summary>Why a playbook is not playable yet: one reason per kind it cannot play.</summary>
    /// <remarks>
    /// Asked of the playbook rather than found while running it, so one run names every kind the
    /// runner cannot play.
    /// </remarks>
    /// <param name="context">The playbook a case loads.</param>
    /// <returns>The reasons, in the order the kinds appear, each named once.</returns>
    public static IEnumerable<string> WhyNotPlayable(PlayContext context) =>
        context.Playbook.Nodes
            .Where(node => !CanPlay(node))
            .Select(node => node.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .Select(SessionReasons.UnplayableNodeKind);

    /// <summary>Why a session entry cannot be taken, or nothing when it can.</summary>
    /// <param name="entry">The entry to ask about.</param>
    /// <returns>The reasons, in the order the entry names them, each named once.</returns>
    public static IEnumerable<string> WhyNotPlayable(SessionEntry entry) => entry switch
    {
        Send send when !CanPlay(send) => [SessionReasons.UnsendableCommand(Commands.NameOf(send))],
        Expect expect => expect.Message.AsObject()
            .Select(claim => claim.Key)
            .Where(claim => !ExpectationMatchers.CanCheck(claim))
            .Distinct(StringComparer.Ordinal)
            .Select(SessionReasons.UncheckableClaim),
        _ => [],
    };

    /// <summary>
    /// Why a session is not playable yet: one reason per send no reader owns, and per claim the
    /// harness cannot check.
    /// </summary>
    /// <remarks>
    /// Asked of the session before running it. A run cannot carry on past an unsendable message:
    /// it would stand still, and later entries would be checked against a state the fixture never
    /// described.
    /// </remarks>
    /// <param name="session">What a case sends and expects.</param>
    /// <returns>The reasons, in the order the session names them, each named once.</returns>
    public static IEnumerable<string> WhyNotPlayable(ImmutableArray<SessionEntry> session) =>
        session.SelectMany(WhyNotPlayable).Distinct(StringComparer.Ordinal);
}
