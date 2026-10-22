using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;
using static DialogueDown.Runtime.Tests.EventFactory;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class ResolveMatcherTests
{
    private readonly ResolveMatcher _matcher = new();

    [Fact]
    public void TheKeysTheFixtureNamesConform() =>
        AssertConformed(Match(Asked("Alice.HasKey"), """[ "Alice.HasKey" ]"""));

    [Fact]
    public void KeysAskedInAnotherOrderDiverge() =>
        // A run names the keys in a set order, truths then words then numbers, each as the playbook
        // names them, so runtimes reading one playbook ask in one order, which a fixture can check.
        AssertDiverged(
            Match(Asked("Alice.HasKey", "playerName"), """[ "playerName", "Alice.HasKey" ]"""),
            """expected ["playerName","Alice.HasKey"] to be asked about, but ["Alice.HasKey","playerName"] was""");

    [Fact]
    public void AKeyTheRunNeverAskedAboutDiverges() =>
        AssertDiverged(
            Match(Asked("Alice.HasKey"), """[ "Bob.HasRope" ]"""),
            """expected ["Bob.HasRope"] to be asked about, but ["Alice.HasKey"] was""");

    [Fact]
    public void ARunSpeakingInsteadOfAskingDiverges() =>
        AssertDiverged(
            Match(Said("Alice", "Hi"), """[ "Alice.HasKey" ]"""),
            "expected the world to be asked something, but the run heard Alice speak");

    [Fact]
    public void AskingIsDescribedByTheKeysWhenSomethingElseWasExpected() =>
        // The description is what a divergence quotes, so a reader of the failure sees which keys
        // went out rather than only that something did.
        AssertDiverged(
            new EndedMatcher().Match(Asked("Alice.HasKey"), Json("true")),
            "expected the run to end, but it asked the world about Alice.HasKey");

    private static Resolve Asked(params string[] keys) => new([.. keys]);

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, Json(expected));
}
