using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>
/// Checks whether one of a runner's events satisfies one shape an expectation can claim.
/// </summary>
/// <remarks>
/// One implementation per key an expectation object can carry, so each shape's matching logic
/// lives in its own small, focused type.
/// </remarks>
internal interface IExpectationMatcher
{
    /// <summary>Gets the key in an expectation this matcher owns, e.g. "said".</summary>
    string Key { get; }

    /// <summary>Checks whether <paramref name="happened"/> satisfies what <paramref name="expected"/> claims.</summary>
    /// <param name="happened">What the runner actually said.</param>
    /// <param name="expected">The value at this matcher's <see cref="Key"/> in the expectation.</param>
    /// <returns>What this one claim made of the event.</returns>
    SessionOutcome Match(Event happened, JsonNode expected);
}
