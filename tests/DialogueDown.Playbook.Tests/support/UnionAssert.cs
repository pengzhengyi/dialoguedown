using System.Reflection;
using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Tests.Support;

/// <summary>
/// Assertions that a tagged union stays whole: every member registered, every wire tag declared.
/// </summary>
/// <remarks>
/// Registration is easy to forget and fails late — a member with no <c>JsonDerivedType</c> throws
/// only when something tries to serialize it, in whatever host got there first. Checking it by
/// reflection turns that into a build failure naming the missing type.
/// </remarks>
internal static class UnionAssert
{
    /// <summary>
    /// Asserts that the constants on <paramref name="kinds"/>, the <c>JsonDerivedType</c>
    /// registrations on <typeparamref name="TUnion"/>, and the union's concrete members all
    /// describe the same set.
    /// </summary>
    /// <typeparam name="TUnion">The union's base type.</typeparam>
    /// <param name="kinds">The class declaring the union's wire tags as string constants.</param>
    public static void AssertEveryMemberIsTagged<TUnion>(Type kinds)
    {
        var declaredTags = DeclaredTags(kinds);
        var registrations = Registrations<TUnion>();

        Assert.Equal(declaredTags, registrations.Keys.Order().ToList());
        Assert.Equal(ConcreteMembers<TUnion>(), registrations.Values.Select(type => type.Name).Order().ToList());
    }

    /// <summary>
    /// Asserts that <paramref name="samples"/> include one of every concrete member of
    /// <typeparamref name="TUnion"/>, apart from those deliberately left out.
    /// </summary>
    /// <typeparam name="TUnion">The union's base type.</typeparam>
    /// <param name="samples">One value per member, as fed to the test doing the examining.</param>
    /// <param name="except">
    /// Members the examination deliberately leaves out. Naming them keeps the exclusion a decision
    /// on the record rather than an omission, and a name that stops being a member fails here.
    /// </param>
    /// <remarks>
    /// Reach for this where a test examines the union one member at a time. The examination is only
    /// as complete as the list it walks, and a member added later joins the union without joining
    /// that list; this says so at build time instead of leaving the new member untested.
    /// </remarks>
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

    private static List<string> DeclaredTags(Type kinds) =>
        [.. kinds.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Order()];

    private static Dictionary<string, Type> Registrations<TUnion>() =>
        typeof(TUnion).GetCustomAttributes<JsonDerivedTypeAttribute>()
            .ToDictionary(
                attribute => (string)attribute.TypeDiscriminator!,
                attribute => attribute.DerivedType);

    private static List<string> ConcreteMembers<TUnion>() =>
        [.. typeof(TUnion).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(TUnion).IsAssignableFrom(type))
            .Select(type => type.Name)
            .Order()];
}
