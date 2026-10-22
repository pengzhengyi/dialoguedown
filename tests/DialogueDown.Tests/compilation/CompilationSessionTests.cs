using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Diagnostics.Errors;
using static DialogueDown.Tests.Support.ConfigurationFactory;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationSessionTests
{
    [Fact]
    public void Start_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => CompilationSession.Start(null!, CompilationSessionOptions(CompilationMode.StageBoundary)));

    [Fact]
    public void Start_NullOptions_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CompilationSession.Start("x", null!));

    [Fact]
    public void Context_CarriesTheSource() =>
        Assert.Equal(
            "Alice: hi",
            Start(CompilationMode.StageBoundary, "Alice: hi").Context.Source);

    [Fact]
    public void Diagnostics_ExposesWhatWasReported()
    {
        var session = Start(CompilationMode.BestEffort);

        var warning = Report(session, DiagnosticSeverity.Warning);

        Assert.Equal([warning], session.Diagnostics);
    }

    [Fact]
    public void FailFast_ReportingAnError_ThrowsThroughTheSink()
    {
        var session = Start(CompilationMode.FailFast);

        Assert.Throws<DiagnosticException>(() => Report(session, DiagnosticSeverity.Error));
    }

    [Theory]
    [InlineData(CompilationMode.StageBoundary, true)]
    [InlineData(CompilationMode.BestEffort, false)]
    public void ShouldHalt_AfterAnError_DependsOnMode(CompilationMode mode, bool expected)
    {
        var session = Start(mode);

        Report(session, DiagnosticSeverity.Error);

        Assert.Equal(expected, session.ShouldHalt);
    }

    [Fact]
    public void ShouldHalt_WithOnlyAWarning_IsFalse()
    {
        var session = Start(CompilationMode.StageBoundary);

        Report(session, DiagnosticSeverity.Warning);

        Assert.False(session.ShouldHalt);
    }

    [Fact]
    public void Diagnostics_ReportedWithADisabledCode_AreMarkedDisabled()
    {
        var nesting = DiagnosticCatalog.DeeplyNestedChoiceBranch;
        var session = CompilationSession.Start(
            "x", CompilationSessionOptions(CompilationMode.StageBoundary, nesting.Code));

        var reported = Report(session, Diagnostic(nesting));

        Assert.Equal([reported with { IsDisabled = true }], session.Diagnostics);
    }

    [Fact]
    public void Diagnostics_WithNothingDisabled_AreLeftEnabled()
    {
        var session = Start(CompilationMode.StageBoundary);

        var warning = Report(session, DiagnosticSeverity.Warning);

        Assert.Equal([warning], session.Diagnostics);
    }

    private static CompilationSession Start(CompilationMode mode, string source = "x") =>
        CompilationSession.Start(source, CompilationSessionOptions(mode));

    // Each overload reports a diagnostic through the session's sink and returns it, so a test can
    // also assert on the exact instance that was collected.
    private static Diagnostic Report(CompilationSession session, DiagnosticSeverity severity) =>
        Report(session, Diagnostic(severity: severity));

    private static Diagnostic Report(CompilationSession session, Diagnostic diagnostic)
    {
        session.Context.Diagnostics.Report(diagnostic);
        return diagnostic;
    }
}
