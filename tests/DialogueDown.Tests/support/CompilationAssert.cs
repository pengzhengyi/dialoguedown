using DialogueDown.Compilation;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions over what one compile produced.</summary>
internal static class CompilationAssert
{
    /// <summary>Asserts the compile ran every stage, and returns the artifacts it produced.</summary>
    public static CompilationSuccess AssertSuccess(CompilationResult result) =>
        Assert.IsType<CompilationSuccess>(result);

    /// <summary>Asserts the compile stopped early, and returns how far it got.</summary>
    public static CompilationFailure AssertFailure(CompilationResult result) =>
        Assert.IsType<CompilationFailure>(result);
}
