using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class ResolveMatcherTests
{
    private readonly ResolveMatcher _matcher = new();

    [Fact]
    public void TheKeysTheFixtureNamesConform() =>
        AssertConformed(Match(Asked("Alice.HasKey"), """[ "Alice.HasKey" ]"""));

    [Fact]
    public void KeysAskedInAnotherOrderDiverge() =>
        // A run names keys as the playbook names them, so two runtimes reading one playbook ask in
        // one order and a fixture can hold them to it.
        AssertDiverged(
            Match(Asked("Alice.HasKey", "playerName"), """[ "playerName", "Alice.HasKey" ]"""),
            "playerName");

    [Fact]
    public void AKeyTheRunNeverAskedAboutDiverges() =>
        AssertDiverged(Match(Asked("Alice.HasKey"), """[ "Bob.HasRope" ]"""), "Bob.HasRope");

    [Fact]
    public void ARunSpeakingInsteadOfAskingDiverges() =>
        AssertDiverged(
            Match(Spoke(), """[ "Alice.HasKey" ]"""),
            "expected the world to be asked something",
            "heard Alice speak");

    [Fact]
    public void AskingIsDescribedByTheKeysWhenSomethingElseWasExpected() =>
        // The description is what a divergence quotes, so a reader of the failure sees which keys
        // went out rather than only that something did.
        AssertDiverged(
            new EndedMatcher().Match(Asked("Alice.HasKey"), JsonNode.Parse("true")!),
            "asked the world about Alice.HasKey");

    private static Resolve Asked(params string[] keys) => new([.. keys]);

    private static Said Spoke() =>
        new("Alice", ImmutableArray.Create<SpeechFragment>(new TextFragment("Hi")));

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, JsonNode.Parse(expected)!);
}
