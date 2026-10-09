using System.Text.Json.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class PerformMatcherTests
{
    [Fact]
    public void PerformingIsDescribedByTheEffectWhenSomethingElseWasExpected() =>
        // The effect is written as a fixture claims it, so a reader can set the two side by side.
        AssertDiverged(
            new EndedMatcher().Match(new Perform(new CustomCommandFragment("Wave", [])), JsonNode.Parse("{}")!),
            """asked the host to perform {"kind":"custom-command","name":"Wave","args":[]}""");
}
