using System.Reflection;
using System.Runtime.CompilerServices;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group E — namespace layout. An assembly's root namespace should carry its
/// façade, the entry points a consumer calls, while everything else lives in a
/// sub-namespace that names its role. A crowded root namespace is where types
/// land when nobody decided where they belong, so this caps it.
/// </summary>
/// <remarks>
/// Only the <em>root</em> namespace is capped, because a deep namespace can be
/// large and healthy: <c>DialogueDown.Script.Ast</c> holds a whole node
/// vocabulary at one level, and splitting it would invent categories the domain
/// does not have. A cap on every namespace would flag that before it flagged a
/// genuinely flat layer, and would need an exemption list holding the largest
/// namespaces. The count deliberately ignores visibility: the core is almost
/// entirely internal, so counting only public types would make this a no-op.
/// </remarks>
public sealed class NamespaceLayoutTests
{
    /// <summary>Maximum types an assembly's root namespace may hold directly.</summary>
    private const int MaxTypesPerRootNamespace = 8;

    [Fact]
    public void RootNamespaces_DoNotHoldTooManyTypes()
    {
        var offenders = Architecture.AllAssemblies
            .Select(assembly => (name: RootNamespaceOf(assembly), types: RootTypesOf(assembly)))
            .Where(entry => entry.types.Count > MaxTypesPerRootNamespace)
            .OrderByDescending(entry => entry.types.Count)
            .Select(entry =>
                $"  - {entry.name} holds {entry.types.Count} types " +
                $"(for example {string.Join(", ", entry.types.Take(3).Select(type => type.Name))})")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Root namespaces exceeding {MaxTypesPerRootNamespace} types " +
                "(move types into sub-namespaces that name their role):" +
                Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static string RootNamespaceOf(Assembly assembly) => assembly.GetName().Name!;

    private static IReadOnlyList<Type> RootTypesOf(Assembly assembly)
    {
        var root = RootNamespaceOf(assembly);
        return assembly.GetTypes()
            .Where(type => type.Namespace == root && IsAuthored(type))
            .OrderBy(type => type.Name)
            .ToList();
    }

    // A nested type belongs to its parent rather than to the namespace, and a
    // compiler-generated type is not the author's layout at all.
    private static bool IsAuthored(Type type) =>
        !type.IsNested &&
        !type.Name.Contains('<') &&
        !Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
}
