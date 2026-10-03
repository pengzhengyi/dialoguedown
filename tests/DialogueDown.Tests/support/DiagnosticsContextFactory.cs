using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds a <see cref="DiagnosticsContext"/>, so a stage call in a test threads a
/// context through one place with sane defaults (an empty source and a fresh
/// <see cref="DiagnosticBag"/>) and the stage signature change touches only this file.
/// </summary>
internal static class DiagnosticsContextFactory
{
    public static DiagnosticsContext Context(string source = "", IDiagnosticSink? diagnostics = null) =>
        new(source, diagnostics ?? new DiagnosticBag());

    /// <summary>
    /// Builds a context over a fresh <see cref="DiagnosticBag"/>, returning the bag so a test can
    /// assert what a stage reported through the context.
    /// </summary>
    public static DiagnosticsContext Context(out DiagnosticBag diagnostics, string source = "")
    {
        diagnostics = new DiagnosticBag();
        return new DiagnosticsContext(source, diagnostics);
    }
}
