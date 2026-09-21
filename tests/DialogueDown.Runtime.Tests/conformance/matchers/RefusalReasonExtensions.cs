using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>
/// What a fixture calls a refusal reason: the one name a reason answers to, and whether a name is
/// that reason's.
/// </summary>
/// <remarks>
/// The spelling is the fixture format's: the schema declares it and the corpus's own fixtures pin
/// it, which is why the runtime's enum carries no serialization attribute — the runtime depends on
/// the playbook and the framework and nothing else, and an architecture test holds it that way.
/// </remarks>
internal static class RefusalReasonExtensions
{
    /// <summary>The name a fixture gives a reason.</summary>
    /// <param name="reason">The reason.</param>
    /// <returns>The name a fixture writes for it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">No name is written for the reason.</exception>
    public static string Name(this RefusalReason reason) => reason switch
    {
        RefusalReason.NotStarted => "not-started",
        RefusalReason.AlreadyEnded => "already-ended",
        RefusalReason.Misplaced => "misplaced",
        RefusalReason.UnknownCommand => "unknown-command",
        RefusalReason.LeadsNowhere => "leads-nowhere",
        RefusalReason.EndlessRing => "endless-ring",
        RefusalReason.UnansweredCondition => "unanswered-condition",
        RefusalReason.UnansweredKey => "unanswered-key",
        RefusalReason.UnaskedKey => "unasked-key",
        RefusalReason.WrongAnswerKind => "wrong-answer-kind",
        RefusalReason.UnplayableNode => "unplayable-node",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "No name is written for this reason."),
    };

    /// <summary>Whether a name a fixture wrote is this reason's.</summary>
    /// <param name="reason">The reason a run reported.</param>
    /// <param name="claimed">The name a fixture wrote.</param>
    /// <returns><see langword="true"/> only when the two are the pair.</returns>
    public static bool Matches(this RefusalReason reason, string claimed) => reason.Name() == claimed;
}
