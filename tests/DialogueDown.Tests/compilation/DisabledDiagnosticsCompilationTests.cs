using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using static DialogueDown.Tests.Support.DiagnosticsAssert;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// A project's disabled codes reach a real compile through each composition root: a disabled
/// diagnostic is still reported, with its fixes, but marked disabled, and an error still fails the
/// compile. Guards the wiring from <see cref="CompilerOptions"/> into the compilation session.
/// </summary>
public sealed class DisabledDiagnosticsCompilationTests
{
    // A dangling arrow, a warning with a fix, and a duplicated scene heading, an error.
    private const string Script = """
        # The workshop

        => The market

        # The workshop

        Alice: Again.
        """;

    private static readonly CompilerOptions _disablesTheArrow =
        CompilerOptions.Default with { DisabledDiagnostics = [DiagnosticCatalog.DanglingJumpArrow.Code] };

    [Fact]
    public void CreateDefault_ADisabledCode_IsReportedMarkedDisabledWithItsFix()
    {
        var result = ScriptCompilerFactory.CreateDefault(_disablesTheArrow).Compile(Script);

        var arrow = AssertLocated(result.LocatedDiagnostics, DiagnosticCatalog.DanglingJumpArrow);
        Assert.True(arrow.IsDisabled);
        Assert.NotEmpty(arrow.Fixes);
    }

    [Fact]
    public void CreateDefault_ADisabledCode_LeavesTheErrorEnabledAndTheCompileFailed()
    {
        var result = ScriptCompilerFactory.CreateDefault(_disablesTheArrow).Compile(Script);

        var duplicate = AssertLocated(result.LocatedDiagnostics, DiagnosticCatalog.DuplicateAnchor);
        Assert.False(duplicate.IsDisabled);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void CreateDefault_Unconfigured_DisablesNothing() =>
        Assert.All(
            ScriptCompilerFactory.CreateDefault().Compile(Script).LocatedDiagnostics,
            diagnostic => Assert.False(diagnostic.IsDisabled));

    [Fact]
    public void AddDialogueDown_ADisabledCode_IsReportedMarkedDisabled()
    {
        // The container root must honor the setting too, or the two roots would disagree.
        using var provider = new ServiceCollection()
            .AddDialogueDown(_disablesTheArrow)
            .BuildServiceProvider();

        var result = provider.GetRequiredService<IScriptCompiler>().Compile(Script);

        Assert.True(AssertLocated(result.LocatedDiagnostics, DiagnosticCatalog.DanglingJumpArrow).IsDisabled);
    }
}
