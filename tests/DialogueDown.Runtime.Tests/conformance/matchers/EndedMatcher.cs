using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks an <c>ended</c> claim: that the run finished here.</summary>
internal sealed class EndedMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "ended";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Ended
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged($"expected the run to end, but it {happened.Describe()}");
}
