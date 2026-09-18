using System.Reflection;
using System.Text.Json.Serialization;
using DialogueDown.TestSupport;

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
        Assert.Equal(
            UnionMembers.NamesOf<TUnion>().Order().ToList(),
            registrations.Values.Select(type => type.Name).Order().ToList());
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
}
