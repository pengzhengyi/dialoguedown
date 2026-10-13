using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Diagnostics.Errors;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationSessionTests
{
    [Fact]
    public void Start_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => CompilationSession.Start(null!, CompilationPolicy.Of(CompilationMode.StageBoundary)));

    [Fact]
    public void Start_NullPolicy_Throws() =>
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
        var session = CompilationSession.Start(
            "x", new CompilationPolicy(CompilationMode.StageBoundary, new HashSet<string> { "DLG3002" }));
        var nesting = Diagnostic(
            Descriptor("DLG3002", DiagnosticCategory.Style, defaultSeverity: DiagnosticSeverity.Warning));

        session.Context.Diagnostics.Report(nesting);

        Assert.Equal([nesting with { IsDisabled = true }], session.Diagnostics);
    }

    [Fact]
    public void Diagnostics_WithNothingDisabled_AreLeftEnabled()
    {
        var session = Start(CompilationMode.StageBoundary);

        var warning = Report(session, DiagnosticSeverity.Warning);

        Assert.False(Assert.Single(session.Diagnostics).IsDisabled);
        Assert.Equal([warning], session.Diagnostics);
    }

    private static CompilationSession Start(CompilationMode mode, string source = "x") =>
        CompilationSession.Start(source, CompilationPolicy.Of(mode));

    // Reports a diagnostic of the given severity through the session's sink, returning it so a test
    // can also assert on the exact instance that was collected.
    private static Diagnostic Report(CompilationSession session, DiagnosticSeverity severity)
    {
        var diagnostic = Diagnostic(severity: severity);
        session.Context.Diagnostics.Report(diagnostic);
        return diagnostic;
    }
}
