using DialogueDown.Compilation;
using DialogueDown.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// A project's disabled codes reach a real compile through each composition root: a disabled
/// diagnostic is still reported, with its fixes, but marked disabled, and an error still fails the
/// compile. Guards the wiring from <see cref="CompilerOptions"/> into the compilation session.
/// </summary>
public sealed class DisabledDiagnosticsCompilationTests
{
    // A dangling arrow (DLG1113, a warning with a fix) and a duplicated scene heading (DLG2001, an
    // error).
    private const string Script = """
        # The workshop

        => The market

        # The workshop

        Alice: Again.
        """;

    private static readonly CompilerOptions _disablesTheArrow =
        CompilerOptions.Default with { DisabledDiagnostics = ["DLG1113"] };

    [Fact]
    public void CreateDefault_ADisabledCode_IsReportedMarkedDisabledWithItsFix()
    {
        var result = ScriptCompilerFactory.CreateDefault(_disablesTheArrow).Compile(Script);

        var arrow = Assert.Single(result.LocatedDiagnostics, diagnostic => diagnostic.Code == "DLG1113");
        Assert.True(arrow.IsDisabled);
        Assert.NotEmpty(arrow.Fixes);
    }

    [Fact]
    public void CreateDefault_ADisabledCode_LeavesTheErrorEnabledAndTheCompileFailed()
    {
        var result = ScriptCompilerFactory.CreateDefault(_disablesTheArrow).Compile(Script);

        var duplicate = Assert.Single(result.LocatedDiagnostics, diagnostic => diagnostic.Code == "DLG2001");
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

        Assert.True(Assert.Single(result.LocatedDiagnostics, diagnostic => diagnostic.Code == "DLG1113").IsDisabled);
    }
}
