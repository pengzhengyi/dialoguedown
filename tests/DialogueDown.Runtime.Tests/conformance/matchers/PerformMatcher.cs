using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>perform</c> claim: what the run asked the host to carry out.</summary>
/// <remarks>
/// The effect is compared as the playbook writes it, so a fixture names an effect in the same
/// words the document does and a runtime that renamed one on the way out is caught.
/// </remarks>
internal sealed class PerformMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "perform";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Perform perform
            ? MatchEffect(perform, expected)
            : SessionOutcome.Diverged(
                $"expected the host to be asked to carry something out, but the run {happened.Describe()}");

    private static SessionOutcome MatchEffect(Perform perform, JsonNode expected)
    {
        var asked = JsonSerializer.SerializeToNode(perform.Effect, FixtureJson.Compact);

        return JsonNode.DeepEquals(asked, expected)
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected {expected.ToJsonString()} to be carried out, but {asked?.ToJsonString()} was asked for");
    }
}
