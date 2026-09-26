namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class SessionOutcomeTests
{
    [Fact]
    public void Combine_OfNothing_Conforms()
    {
        Assert.True(SessionOutcome.Combine([]).IsConformed);
    }

    [Fact]
    public void Combine_OfConformingOutcomes_Conforms()
    {
        var combined = SessionOutcome.Combine([SessionOutcome.Conformed(), SessionOutcome.Conformed()]);

        Assert.True(combined.IsConformed);
    }

    [Fact]
    public void Combine_WithOneDivergence_Diverges()
    {
        var combined = SessionOutcome.Combine([
            SessionOutcome.Conformed(),
            SessionOutcome.Diverged("said the wrong thing"),
            SessionOutcome.Conformed()]);

        Assert.Equal(SessionVerdict.Diverged, combined.Verdict);
        Assert.Equal("said the wrong thing", combined.Because);
    }

    [Fact]
    public void Combine_WithOneUnplayableClaim_IsNotYetPlayable()
    {
        var combined = SessionOutcome.Combine([
            SessionOutcome.Conformed(),
            SessionOutcome.NotYetPlayable("nothing checks describe yet")]);

        Assert.Equal(SessionVerdict.NotYetPlayable, combined.Verdict);
    }

    [Fact]
    public void Combine_ADivergenceOutranksAnUnplayableClaim()
    {
        // Otherwise a real failure hides behind an unrelated check nobody has taught the harness.
        var combined = SessionOutcome.Combine([
            SessionOutcome.NotYetPlayable("nothing checks describe yet"),
            SessionOutcome.Diverged("said the wrong thing")]);

        Assert.Equal(SessionVerdict.Diverged, combined.Verdict);
        Assert.Equal("said the wrong thing", combined.Because);
    }

    [Fact]
    public void Combine_OfTwoDivergences_GathersBoth()
    {
        // A contributor fixes one divergence and re-runs; gathering means meeting both at once.
        var combined = SessionOutcome.Combine([
            SessionOutcome.Diverged("the speaker"),
            SessionOutcome.Diverged("the speech")]);

        Assert.Equal(["the speaker", "the speech"], combined.Reasons);
    }

    [Fact]
    public void Combine_OfTwoUnplayableClaims_GathersBoth()
    {
        // What lets one run name every construct this build has yet to learn.
        var combined = SessionOutcome.Combine([
            SessionOutcome.NotYetPlayable("nothing checks describe yet"),
            SessionOutcome.NotYetPlayable("nothing checks asked yet")]);

        Assert.Equal(SessionVerdict.NotYetPlayable, combined.Verdict);
        Assert.Equal(["nothing checks describe yet", "nothing checks asked yet"], combined.Reasons);
    }

    [Fact]
    public void Combine_DropsTheReasonsOfAVerdictItOutranks()
    {
        var combined = SessionOutcome.Combine([
            SessionOutcome.NotYetPlayable("nothing checks describe yet"),
            SessionOutcome.Diverged("the speaker"),
            SessionOutcome.Diverged("the speech")]);

        Assert.Equal(["the speaker", "the speech"], combined.Reasons);
    }

    [Fact]
    public void Combine_ReadsEveryPartial()
    {
        var read = 0;

        var combined = SessionOutcome.Combine(Counted());

        Assert.Equal(SessionVerdict.Diverged, combined.Verdict);
        Assert.Equal(3, read);

        IEnumerable<SessionOutcome> Counted()
        {
            foreach (var outcome in new[]
            {
                SessionOutcome.Conformed(),
                SessionOutcome.Diverged("said the wrong thing"),
                SessionOutcome.Diverged("never read before"),
            })
            {
                read++;
                yield return outcome;
            }
        }
    }

    [Fact]
    public void Combine_ReadsOnPastAClaimItCannotCheck()
    {
        // A divergence outranks one, so reading carries on until it is known there is none.
        var combined = SessionOutcome.Combine([
            SessionOutcome.NotYetPlayable("nothing checks describe yet"),
            SessionOutcome.Conformed(),
            SessionOutcome.Diverged("said the wrong thing")]);

        Assert.Equal(SessionVerdict.Diverged, combined.Verdict);
    }

    [Fact]
    public void IsConformed_IsTrueOnlyForAConformingOutcome()
    {
        Assert.True(SessionOutcome.Conformed().IsConformed);
        Assert.False(SessionOutcome.Diverged("nope").IsConformed);
        Assert.False(SessionOutcome.NotYetPlayable("not yet").IsConformed);
    }

    [Fact]
    public void Because_OfNothing_SaysNothingDiverged()
    {
        Assert.Equal("nothing diverged", SessionOutcome.Conformed().Because);
    }

    [Fact]
    public void Because_OfOneReason_IsThatReason()
    {
        Assert.Equal("the speaker", SessionOutcome.Diverged("the speaker").Because);
    }

    [Fact]
    public void Because_OfSeveralReasons_CountsAndListsThem()
    {
        var combined = SessionOutcome.Combine([
            SessionOutcome.Diverged("the speaker"),
            SessionOutcome.Diverged("the speech")]);

        Assert.Equal(
            $"2 reasons:{Environment.NewLine}  - the speaker{Environment.NewLine}  - the speech",
            combined.Because);
    }
}
