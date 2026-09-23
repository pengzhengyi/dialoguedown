using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>resolve</c> claim: what the run asked the world about.</summary>
/// <remarks>
/// The keys are compared in order, because a run names them as the playbook names them and two
/// runtimes reading the same playbook therefore ask in the same order. A fixture that listed them
/// some other way would be claiming something the protocol does not promise.
/// </remarks>
internal sealed class ResolveMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "resolve";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Resolve resolve
            ? MatchKeys(resolve, expected)
            : SessionOutcome.Diverged(
                $"expected the world to be asked something, but the run {happened.Describe()}");

    private static SessionOutcome MatchKeys(Resolve resolve, JsonNode expected)
    {
        var asked = JsonSerializer.SerializeToNode(resolve.Keys, FixtureJson.Compact);

        return JsonNode.DeepEquals(asked, expected)
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected {expected.ToJsonString()} to be asked about, but {asked?.ToJsonString()} was");
    }
}
