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
    public void Combine_WithOneUnrunnableClaim_IsNotYetRunnable()
    {
        var combined = SessionOutcome.Combine([
            SessionOutcome.Conformed(),
            SessionOutcome.NotYetRunnable("nothing checks resolve yet")]);

        Assert.Equal(SessionVerdict.NotYetRunnable, combined.Verdict);
    }

    [Fact]
    public void Combine_ADivergenceOutranksAnUnrunnableClaim()
    {
        // Otherwise a real failure hides behind an unrelated check nobody has taught the harness.
        var combined = SessionOutcome.Combine([
            SessionOutcome.NotYetRunnable("nothing checks resolve yet"),
            SessionOutcome.Diverged("said the wrong thing")]);

        Assert.Equal(SessionVerdict.Diverged, combined.Verdict);
        Assert.Equal("said the wrong thing", combined.Because);
    }

    [Fact]
    public void Combine_OfTwoDivergences_ReportsTheFirst()
    {
        var combined = SessionOutcome.Combine([
            SessionOutcome.Diverged("the speaker"),
            SessionOutcome.Diverged("the speech")]);

        Assert.Equal("the speaker", combined.Because);
    }

    [Fact]
    public void Combine_StopsReadingAtTheFirstDivergence()
    {
        var read = 0;

        var combined = SessionOutcome.Combine(Counted());

        Assert.Equal(SessionVerdict.Diverged, combined.Verdict);
        Assert.Equal(2, read);

        IEnumerable<SessionOutcome> Counted()
        {
            foreach (var outcome in new[]
            {
                SessionOutcome.Conformed(),
                SessionOutcome.Diverged("said the wrong thing"),
                SessionOutcome.Diverged("never read"),
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
            SessionOutcome.NotYetRunnable("nothing checks resolve yet"),
            SessionOutcome.Conformed(),
            SessionOutcome.Diverged("said the wrong thing")]);

        Assert.Equal(SessionVerdict.Diverged, combined.Verdict);
    }

    [Fact]
    public void IsConformed_IsTrueOnlyForAConformingOutcome()
    {
        Assert.True(SessionOutcome.Conformed().IsConformed);
        Assert.False(SessionOutcome.Diverged("nope").IsConformed);
        Assert.False(SessionOutcome.NotYetRunnable("not yet").IsConformed);
    }
}
