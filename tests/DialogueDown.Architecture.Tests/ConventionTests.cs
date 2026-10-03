using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Conventions: every exception type in the core lives in a <c>*.Errors</c> namespace, and every
/// Dialogue AST type is immutable.
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
    /// The transpiler, desugarer, analyzer, and graph builder all read the same AST without
    /// copying it, so a settable property would let a later stage change what an earlier one
    /// produced. <c>BeImmutableExternally</c> checks the publicly reachable state.
    /// <para>
    /// Enums are excluded: the check reads an enum's compiler-generated <c>value__</c> field as
    /// mutable state.
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
