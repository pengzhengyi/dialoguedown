using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class OfferMatcherTests
{
    private const string TwoWays = """
        { "ordered": false, "options": [
            { "label": "Go east", "available": true },
            { "label": "Go west", "available": true } ] }
        """;

    private readonly OfferMatcher _matcher = new();

    [Fact]
    public void AMenuOfferedAsClaimedConforms() =>
        AssertConformed(Match(Bulleted(Option("Go east"), Option("Go west")), TwoWays));

    [Fact]
    public void OptionsOfferedInAnotherOrderDiverge() =>
        // Every menu is offered in the order written, so a bulleted menu is held to that order too.
        AssertDiverged(
            Match(Bulleted(Option("Go west"), Option("Go east")), TwoWays),
            "option 0: expected \"Go east\", but heard \"Go west\"",
            "option 1: expected \"Go west\", but heard \"Go east\"");

    [Fact]
    public void AMenuOfTheOtherKindDiverges() =>
        AssertDiverged(
            Match(Numbered(Option("Go east"), Option("Go west")), TwoWays),
            "expected a bulleted menu, but a numbered one was offered");

    [Fact]
    public void AMenuOfferingAnotherNumberOfOptionsDiverges() =>
        AssertDiverged(
            Match(Bulleted(Option("Go east")), TwoWays),
            "expected 2 options, but the menu offered 1");

    [Fact]
    public void AnOptionOfferedUnavailableWhenClaimedAvailableDiverges() =>
        AssertDiverged(
            Match(Bulleted(Option("Go east", available: false), Option("Go west")), TwoWays),
            "option 0: expected it available, but it was not");

    [Fact]
    public void AnythingButAnOfferDiverges() =>
        AssertDiverged(Match(new Ended(), TwoWays), "expected a menu to be offered", "ended");

    [Fact]
    public void OfferingIsDescribedByItsLabelsWhenSomethingElseWasExpected() =>
        // Each label is quoted, so a label holding a comma still reads as one option.
        AssertDiverged(
            new EndedMatcher().Match(Numbered(Option("Go east"), Option("Wait, listen")), JsonNode.Parse("{}")!),
            "offered a menu: \"Go east\", \"Wait, listen\"");

    [Fact]
    public void AnOfferThatDoesNotSayWhetherItIsOrderedIsAFixtureBug() =>
        Assert.Throws<InvalidFixtureException>(
            () => Match(Bulleted(Option("Go east")), """{ "options": [] }"""));

    /// <summary>A bulleted menu, offered as the run would offer it.</summary>
    /// <param name="options">Its options, in the order written.</param>
    /// <returns>The offer.</returns>
    private static Offer Bulleted(params OfferedOption[] options) => new(Ordered: false, [.. options]);

    /// <summary>A numbered menu, offered as the run would offer it.</summary>
    /// <param name="options">Its options, in the order written.</param>
    /// <returns>The offer.</returns>
    private static Offer Numbered(params OfferedOption[] options) => new(Ordered: true, [.. options]);

    /// <summary>One option of a menu, labelled with plain words.</summary>
    /// <param name="label">What the option says.</param>
    /// <param name="available">Whether the player can take it.</param>
    /// <returns>The option.</returns>
    private static OfferedOption Option(string label, bool available = true) =>
        new([new TextFragment(label)], available);

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, JsonNode.Parse(expected)!);
}
