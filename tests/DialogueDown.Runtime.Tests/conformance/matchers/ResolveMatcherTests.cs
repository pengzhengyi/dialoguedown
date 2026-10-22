using DialogueDown.Conformance;
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
        AssertConformed(Match(Asked(booleanKeys: ["Alice.HasKey"]), """{ "boolean": [ "Alice.HasKey" ] }"""));

    [Fact]
    public void KeysOfEveryKindConform() =>
        AssertConformed(
            Match(
                Asked(booleanKeys: ["Guard.IsAngry"], textKeys: ["playerName"], numberKeys: ["Guard.Mood"]),
                """{ "boolean": [ "Guard.IsAngry" ], "text": [ "playerName" ], "number": [ "Guard.Mood" ] }"""));

    [Fact]
    public void KindsWrittenInAnotherOrderConform() =>
        // A fixture names each kind as a property, and the order properties are written in means
        // nothing in JSON.
        AssertConformed(
            Match(
                Asked(booleanKeys: ["Alice.HasKey"], textKeys: ["playerName"]),
                """{ "text": [ "playerName" ], "boolean": [ "Alice.HasKey" ] }"""));

    [Fact]
    public void KeysOfOneKindAskedInAnotherOrderDiverge() =>
        // A run names each kind's keys as the playbook names them, so runtimes reading one playbook
        // ask in one order, which a fixture can check.
        AssertDiverged(
            Match(
                Asked(booleanKeys: ["Hero.HasSword", "Hero.HasShield"]),
                """{ "boolean": [ "Hero.HasShield", "Hero.HasSword" ] }"""),
            """expected ["Hero.HasShield","Hero.HasSword"] to be asked for a truth, """
            + """but ["Hero.HasSword","Hero.HasShield"] was""");

    [Fact]
    public void AKeyAskedAsAnotherKindDiverges() =>
        // The kind tells the driver what to answer with, so the right key under the wrong kind is
        // a different request, and both kinds it touches are named.
        AssertDiverged(
            Match(Asked(booleanKeys: ["Alice.HasKey"]), """{ "text": [ "Alice.HasKey" ] }"""),
            """expected [] to be asked for a truth, but ["Alice.HasKey"] was""",
            """expected ["Alice.HasKey"] to be asked for words, but [] was""");

    [Fact]
    public void AKindTheFixtureLeavesOutDiverges() =>
        // A fixture leaves out a kind nothing asks for, so leaving one out claims the run asked
        // none of it.
        AssertDiverged(
            Match(
                Asked(booleanKeys: ["Alice.HasKey"], textKeys: ["playerName"]),
                """{ "boolean": [ "Alice.HasKey" ] }"""),
            """expected [] to be asked for words, but ["playerName"] was""");

    [Fact]
    public void AKeyTheRunNeverAskedAboutDiverges() =>
        AssertDiverged(
            Match(Asked(booleanKeys: ["Alice.HasKey"]), """{ "boolean": [ "Bob.HasRope" ] }"""),
            """expected ["Bob.HasRope"] to be asked for a truth, but ["Alice.HasKey"] was""");

    [Fact]
    public void AKeyWeightTheRunNeverAskedAboutDiverges() =>
        AssertDiverged(
            Match(Asked(numberKeys: ["Guard.Mood"]), """{ "number": [ "Guard.Temper" ] }"""),
            """expected ["Guard.Temper"] to be asked for a number, but ["Guard.Mood"] was""");

    [Fact]
    public void AKindWrittenUnderAnotherNameIsAFixtureBug() =>
        // Read by name, a misspelled kind would go unread and leave the kind it meant looking
        // empty, so it is refused rather than passed over.
        Assert.Throws<InvalidFixtureException>(
            () => Match(Asked(booleanKeys: ["Alice.HasKey"]), """{ "truth": [ "Alice.HasKey" ] }"""));

    [Fact]
    public void KeysWrittenWithoutTheirKindsAreAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(
            () => Match(Asked(booleanKeys: ["Alice.HasKey"]), """[ "Alice.HasKey" ]"""));

    [Fact]
    public void AKindsKeysWrittenAsAnythingButStringsAreAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(
            () => Match(Asked(booleanKeys: ["Alice.HasKey"]), """{ "boolean": "Alice.HasKey" }"""));

    [Fact]
    public void ARunSpeakingInsteadOfAskingDiverges() =>
        AssertDiverged(
            Match(Said("Alice", "Hi"), """{ "boolean": [ "Alice.HasKey" ] }"""),
            "expected the world to be asked something, but the run heard Alice speak");

    [Fact]
    public void AskingIsDescribedByTheKeysWhenSomethingElseWasExpected() =>
        // The description is what a divergence quotes, so a reader of the failure sees which keys
        // went out rather than only that something did.
        AssertDiverged(
            new EndedMatcher().Match(Asked(booleanKeys: ["Alice.HasKey"]), Json("true")),
            "expected the run to end, but it asked the world about Alice.HasKey");

    private static Resolve Asked(
        string[]? booleanKeys = null, string[]? textKeys = null, string[]? numberKeys = null) =>
        new(BooleanKeys: [.. booleanKeys ?? []], TextKeys: [.. textKeys ?? []], NumberKeys: [.. numberKeys ?? []]);

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, Json(expected));
}
