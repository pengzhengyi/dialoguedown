using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group C — convention hygiene. Keeps the error vocabulary discoverable: the
/// thrown exception hierarchy lives together under <c>*.Errors</c>, while value
/// types that merely describe a failure (for example a parse-failure record) are
/// deliberately excluded because they are data, not exceptions. It also holds the
/// Dialogue AST to the shape every later stage assumes: immutable nodes.
/// </summary>
public sealed class ConventionTests
{
    [Fact]
    public void ExceptionTypes_ResideIn_AnErrorsNamespace()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .ResideInNamespaceEndingWith(".Errors")
            .GetResult()
            .ShouldPass();
    }

    /// <remarks>
    /// The transpiler, desugarer, analyzer, and graph builder all read the same AST and
    /// none of them copies it defensively, so one settable property would let a later
    /// stage change what an earlier one produced — a class of bug that shows up as a
    /// wrong graph far from its cause. Every node is a record today; this keeps it that
    /// way. <c>BeImmutableExternally</c> checks the publicly reachable state, which is
    /// what a later stage can actually reach.
    /// <para>
    /// Enums are excluded: an enum is immutable by definition, but the check reads the
    /// compiler-generated <c>value__</c> field as mutable state and would report every
    /// one of them.
    /// </para>
    /// </remarks>
    [Fact]
    public void DialogueAstNodes_AreImmutable()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .That()
            .ResideInNamespace(Architecture.ScriptAst)
            .And()
            .AreNotEnums()
            .Should()
            .BeImmutableExternally()
            .GetResult()
            .ShouldPass();
    }
}
