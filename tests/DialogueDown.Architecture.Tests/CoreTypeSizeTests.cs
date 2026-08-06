using System.Reflection;
using System.Runtime.CompilerServices;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group D — type shape. A class that exposes too many public methods tends to
/// carry too many responsibilities (a "God class"). This guards the core against
/// that by capping the public method surface per type.
/// </summary>
/// <remarks>
/// The count is deliberately of <em>public</em> authored methods, not all methods:
/// the size/complexity guardrails (S138 caps a method at 40 lines) actively
/// encourage decomposing behavior into many small <em>private</em> helpers, so
/// counting those would penalize good design. Compiler-generated members, property
/// and event accessors, operators, and the object/record protocol
/// (<c>Equals</c>, <c>GetHashCode</c>, <c>ToString</c>, <c>Deconstruct</c>,
/// <c>PrintMembers</c>) are excluded so records and data types are not flagged.
/// This lives as an architecture test because SonarAnalyzer's S1448 ("too many
/// methods") does not activate through <c>.editorconfig</c> in the pinned version.
/// </remarks>
public sealed class CoreTypeSizeTests
{
    /// <summary>Maximum public methods a single core type may declare.</summary>
    private const int MaxPublicMethodsPerType = 20;

    private static readonly HashSet<string> ObjectProtocol =
    [
        "Equals", "GetHashCode", "ToString", "PrintMembers", "Deconstruct",
    ];

    [Fact]
    public void CoreTypes_DoNotExposeTooManyPublicMethods()
    {
        var offenders = Architecture.CoreAssembly.GetTypes()
            .Where(IsConsideredType)
            .Select(type => (type, count: PublicAuthoredMethodCount(type)))
            .Where(entry => entry.count > MaxPublicMethodsPerType)
            .OrderByDescending(entry => entry.count)
            .Select(entry => $"  - {entry.type.FullName} has {entry.count} public methods")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Types exceeding {MaxPublicMethodsPerType} public methods (split their responsibilities):" +
                Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static bool IsConsideredType(Type type)
    {
        if (Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))) return false;
        if (type.Name.Contains('<') || type.IsEnum || type.IsInterface) return false;
        if (typeof(Delegate).IsAssignableFrom(type)) return false;
        return type.IsClass || type.IsValueType;
    }

    private static int PublicAuthoredMethodCount(Type type) =>
        type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public |
                        BindingFlags.Instance | BindingFlags.Static)
            .Count(method => !method.IsSpecialName &&
                             !method.Name.Contains('<') &&
                             !Attribute.IsDefined(method, typeof(CompilerGeneratedAttribute)) &&
                             !ObjectProtocol.Contains(method.Name));
}
