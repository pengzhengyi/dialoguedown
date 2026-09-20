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
/// A case is screened before it runs, so a construct nobody has taught reads as not yet playable
/// rather than as a divergence. That screen is a second statement of what the runner knows, which
/// is why a test holds the two to each other: were they to disagree, a case would be reported as
/// untaught when it plays, or as a divergence when nobody had taught it.
/// <para>
/// A node kind is playable or not by kind alone, so the answer is a property of the kind rather
/// than of the instance. A session entry is playable when the harness can take it: a send needs a
/// reader, and an expectation is takeable when every claim in it is one the harness can check —
/// an unowned claim ends the run where it is read, so it is a gap the screen can name first.
/// </para>
/// </remarks>
internal static class Playability
{
    /// <summary>Whether this build can play a node of this kind at all.</summary>
    /// <param name="node">The node to ask about.</param>
    /// <returns><see langword="true"/> when arriving at such a node is something this build does.</returns>
    public static bool CanPlay(Node node) =>
        node is LineNode or EndNode or ControlNode;

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
    /// Asked of the playbook rather than found while running it, so one run names everything the
    /// build has yet to learn.
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
        Send send when !CanPlay(send) => [SessionReasons.UnsendableMessage(send.Message.ToJsonString())],
        Expect expect => expect.Message.AsObject()
            .Select(claim => claim.Key)
            .Where(claim => !ExpectationMatchers.CanCheck(claim))
            .Distinct(StringComparer.Ordinal)
            .Select(SessionReasons.UncheckableClaim),
        _ => [],
    };

    /// <summary>Why a session is not playable yet: one reason per send no reader owns.</summary>
    /// <remarks>
    /// Asked of the session rather than found while running it. Carrying on past an unsendable
    /// message would be wrong instead: the run stands still, so later entries would be judged
    /// against a state the fixture never described.
    /// </remarks>
    /// <param name="session">What a case sends and expects.</param>
    /// <returns>The reasons, in the order the session names them, each named once.</returns>
    public static IEnumerable<string> WhyNotPlayable(ImmutableArray<SessionEntry> session) =>
        session.SelectMany(WhyNotPlayable).Distinct(StringComparer.Ordinal);
}
