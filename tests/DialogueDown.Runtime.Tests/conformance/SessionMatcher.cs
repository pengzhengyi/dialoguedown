using System.Collections.Immutable;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Tests.Conformance.Matchers;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// Holds a whole session against a runner, and says where it first diverged.
/// </summary>
/// <remarks>
/// Events are read in the order they come, one per expectation, so the order a runner says things
/// in is part of what a session checks.
/// </remarks>
internal static class SessionMatcher
{
    /// <summary>Holds a session against a runner, from wherever the run currently stands.</summary>
    /// <param name="op">The runner to drive.</param>
    /// <param name="session">What to send, and what to expect back.</param>
    /// <returns>What became of it.</returns>
    public static SessionOutcome Match(SessionOperator op, ImmutableArray<SessionEntry> session)
    {
        // A session that does not say where to begin begins at the entry, so the run is already
        // under way by the time the first expectation is read.
        if (!BeginsItself(session))
        {
            op.Start();
        }

        // Each entry is played against the state the one before it left, so the session stops at
        // the first that does not conform.
        foreach (var entry in session)
        {
            var outcome = MatchEntry(op, entry);

            if (!outcome.IsConformed)
            {
                return outcome;
            }
        }

        return op.UnreadEventCount > 0
            ? SessionOutcome.Diverged(
                $"the session ended, but the run still has {Unmatched(op.UnreadEventCount)} unmatched")
            : SessionOutcome.Conformed();
    }

    private static SessionOutcome MatchEntry(SessionOperator op, SessionEntry entry) =>
        entry switch
        {
            Send send => op.Send(send),
            Expect expect => ExpectationMatchers.Match(op.NextEvent(), expect),
            _ => throw new NotSupportedException(
                $"No match is defined for {entry.GetType().Name}."),
        };

    private static bool BeginsItself(ImmutableArray<SessionEntry> session) =>
        session.FirstOrDefault() is Send first && Commands.IsStart(first);

    private static string Unmatched(int count) => count == 1 ? "1 event" : $"{count} events";
}
