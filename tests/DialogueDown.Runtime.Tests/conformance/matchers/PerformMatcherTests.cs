using System.Text.Json.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class PerformMatcherTests
{
    private const string Waving = """{ "kind": "custom-command", "name": "Wave", "args": [] }""";

    private readonly PerformMatcher _matcher = new();

    [Fact]
    public void AnEffectPerformedAsClaimedConforms() =>
        AssertConformed(Match(Performed("Wave"), Waving));

    [Fact]
    public void AnEffectClaimedWithItsPropertiesInAnotherOrderConforms() =>
        AssertConformed(Match(Performed("Wave"), """{ "args": [], "name": "Wave", "kind": "custom-command" }"""));

    [Fact]
    public void AnEffectRenamedOnTheWayOutDiverges() =>
        AssertDiverged(
            Match(Performed("Bow"), Waving),
            """expected {"kind":"custom-command","name":"Wave","args":[]} to be performed""",
            """but {"kind":"custom-command","name":"Bow","args":[]} was asked for""");

    [Fact]
    public void AnythingButAPerformDiverges() =>
        AssertDiverged(Match(new Ended(), Waving), "expected the host to be asked to perform something", "ended");

    [Fact]
    public void PerformingIsDescribedByTheEffectWhenSomethingElseWasExpected() =>
        // The effect is written as a fixture claims it, so a reader can set the two side by side.
        AssertDiverged(
            new EndedMatcher().Match(Performed("Wave"), JsonNode.Parse("{}")!),
            """asked the host to perform {"kind":"custom-command","name":"Wave","args":[]}""");

    /// <summary>A run asking the host to perform a named command that takes no arguments.</summary>
    /// <param name="command">The command's name.</param>
    /// <returns>The request.</returns>
    private static Perform Performed(string command) => new(new CustomCommandFragment(command, []));

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, JsonNode.Parse(expected)!);
}
