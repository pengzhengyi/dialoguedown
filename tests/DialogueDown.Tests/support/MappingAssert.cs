namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertions that a mapping covers the whole of what it maps.
/// </summary>
/// <remarks>
/// A mapper switches over a closed union, and a missing arm fails only when a script happens to
/// contain that construct — long after the writer shipped. Checking the samples by reflection
/// turns that into a test failure naming the member nobody mapped.
/// </remarks>
internal static class MappingAssert
{
    /// <summary>
    /// Asserts the samples include one of every concrete member of the union, apart from those
    /// deliberately left out.
    /// </summary>
    /// <typeparam name="TUnion">The union being mapped.</typeparam>
    /// <param name="samples">One value per member, as fed to the mapping's own tests.</param>
    /// <param name="except">
    /// Members a mapping deliberately does not accept. Naming them keeps the exclusion a decision
    /// on the record rather than an omission, and a name that stops being a member fails here.
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
        typeof(TUnion).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(TUnion).IsAssignableFrom(type))
            .Select(type => type.Name)
            .Order();
}
