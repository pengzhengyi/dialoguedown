using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks an <c>offer</c> claim: the menu the run offered.</summary>
/// <remarks>
/// Every menu is offered in the order its options were written, so the options are compared in
/// order whether the menu is numbered or not. A label is checked as speech is, so a fixture may
/// claim it as plain text or as fragments.
/// </remarks>
internal sealed class OfferMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "offer";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Offer offer
            ? MatchOffer(offer, expected.AsObject())
            : SessionOutcome.Diverged($"expected a menu to be offered, but the run {happened.Describe()}");

    // Both parts are checked, so an offer wrong in its kind and in its options says both.
    private static SessionOutcome MatchOffer(Offer offer, JsonObject expected)
    {
        var ordered = expected["ordered"]?.GetValue<bool>()
            ?? throw new InvalidFixtureException("An offer needs ordered.");
        var options = expected["options"]?.AsArray()
            ?? throw new InvalidFixtureException("An offer needs options.");

        return SessionOutcome.Combine(MatchKind(offer.Ordered, ordered), MatchOptions(offer.Options, options));
    }

    private static SessionOutcome MatchKind(bool offered, bool claimed) =>
        offered == claimed
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected a {KindOf(claimed)} menu, but a {KindOf(offered)} one was offered");

    private static string KindOf(bool ordered) => ordered ? "numbered" : "bulleted";

    private static SessionOutcome MatchOptions(ImmutableArray<OfferedOption> offered, JsonArray claimed) =>
        offered.Length == claimed.Count
            ? SessionOutcome.Combine(offered.Select((option, at) => MatchOption(option, claimed[at], at)))
            : SessionOutcome.Diverged($"expected {claimed.Count} options, but the menu offered {offered.Length}");

    private static SessionOutcome MatchOption(OfferedOption offered, JsonNode? claimed, int at)
    {
        var option = claimed?.AsObject()
            ?? throw new InvalidFixtureException($"Option {at} of an offer claims nothing.");
        var label = option["label"]
            ?? throw new InvalidFixtureException($"Option {at} of an offer needs a label.");
        var available = option["available"]?.GetValue<bool>()
            ?? throw new InvalidFixtureException($"Option {at} of an offer needs available.");

        var outcome = SessionOutcome.Combine(
            SpeechClaim.Match(offered.Label, label), MatchAvailable(offered.Available, available));

        return ForOption(outcome, at);
    }

    // A reason names the option it is about, so a menu that gets two options wrong says which.
    private static SessionOutcome ForOption(SessionOutcome outcome, int at) =>
        outcome with { Reasons = [.. outcome.Reasons.Select(reason => $"option {at}: {reason}")] };

    private static SessionOutcome MatchAvailable(bool offered, bool claimed) =>
        offered == claimed
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                claimed ? "expected it available, but it was not" : "expected it unavailable, but it was available");
}
