using System.Reflection;
using System.Runtime.CompilerServices;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Namespace layout. An assembly's root namespace should carry its facade, the entry
/// points a consumer calls, while everything else lives in a sub-namespace that names
/// its role, so this caps the number of types in the root namespace.
/// </summary>
/// <remarks>
/// Only the <em>root</em> namespace is capped: a deeper namespace can be large and
/// sound, as <c>DialogueDown.Script.Ast</c> holds a whole node vocabulary at one level.
/// The count includes internal types, because the core is almost entirely internal.
/// </remarks>
public sealed class NamespaceLayoutTests
{
    /// <summary>Maximum types an assembly's root namespace may hold directly.</summary>
    private const int MaxTypesPerRootNamespace = 10;

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
