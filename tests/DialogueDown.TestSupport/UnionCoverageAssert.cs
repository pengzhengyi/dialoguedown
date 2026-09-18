namespace DialogueDown.TestSupport;

/// <summary>
/// Asserts that a test examining a closed family of types looks at every one of them.
/// </summary>
/// <remarks>
/// A test that walks a family one member at a time is only as complete as the list it walks. A
/// member added later joins the family without joining that list, so the new member goes untested
/// and nothing says so. Comparing the list against the family itself turns that into a failure
/// naming the member nobody covered.
/// </remarks>
public static class UnionCoverageAssert
{
    /// <summary>
    /// Asserts the samples include one of every concrete member of the union, apart from those
    /// deliberately left out.
    /// </summary>
    /// <typeparam name="TUnion">The union's base type.</typeparam>
    /// <param name="samples">One value per member, as fed to the test doing the examining.</param>
    /// <param name="except">
    /// Members the test deliberately leaves out. Naming them keeps the exclusion a decision on the
    /// record rather than an omission, and a name that stops being a member fails here.
    /// </param>
    public static void AssertCoversEveryMember<TUnion>(
        IEnumerable<TUnion> samples, params Type[] except)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(except);

        var excluded = except.Select(type => type.Name).Order().ToList();
        Assert.Equal(excluded, ConcreteMembers<TUnion>().Intersect(excluded).Order());

        var covered = samples.Select(sample => sample!.GetType().Name).Distinct().Order();

        Assert.Equal(ConcreteMembers<TUnion>().Except(excluded), covered);
    }

    private static IEnumerable<string> ConcreteMembers<TUnion>() =>
        UnionMembers.NamesOf<TUnion>().Order();
}
