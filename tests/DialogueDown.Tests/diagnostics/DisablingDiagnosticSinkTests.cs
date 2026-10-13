using DialogueDown.Diagnostics;
using static DialogueDown.Tests.Support.DiagnosticsFactory;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DisablingDiagnosticSinkTests
{
    private static readonly DiagnosticDescriptor _nesting =
        Descriptor("DLG3002", DiagnosticCategory.Style, defaultSeverity: DiagnosticSeverity.Warning);

    [Fact]
    public void Constructor_NullInner_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new DisablingDiagnosticSink(null!, new HashSet<string>()));

    [Fact]
    public void Constructor_NullCodes_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new DisablingDiagnosticSink(new DiagnosticBag(), null!));

    [Fact]
    public void Report_NullDiagnostic_Throws() =>
        Assert.Throws<ArgumentNullException>(() => Sink(out _, "DLG3002").Report(null!));

    [Fact]
    public void Report_ADisabledCode_ForwardsItMarkedDisabled()
    {
        var sink = Sink(out var collected, "DLG3002");
        var nesting = Diagnostic(_nesting);

        sink.Report(nesting);

        Assert.Equal([nesting with { IsDisabled = true }], collected.Diagnostics);
    }

    [Fact]
    public void Report_AnotherCode_ForwardsItUnchanged()
    {
        var sink = Sink(out var collected, "DLG3004");
        var nesting = Diagnostic(_nesting);

        sink.Report(nesting);

        Assert.Same(nesting, Assert.Single(collected.Diagnostics));
    }

    [Fact]
    public void Report_AnErrorWithADisabledCode_IsNeverMarked()
    {
        // A producer may raise a diagnostic above its descriptor's default; an error still fails
        // the compile, so it must stay visible.
        var sink = Sink(out var collected, "DLG3002");
        var error = Diagnostic(_nesting, severity: DiagnosticSeverity.Error);

        sink.Report(error);

        Assert.False(Assert.Single(collected.Diagnostics).IsDisabled);
    }

    private static DisablingDiagnosticSink Sink(out DiagnosticBag collected, params string[] codes)
    {
        collected = new DiagnosticBag();
        return new DisablingDiagnosticSink(collected, new HashSet<string>(codes));
    }
}
