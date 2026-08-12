using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

// Reader-facing documentation for a diagnostic: a plain explanation and, where the producer exists,
// a compiler-verified example pair. This lives test-side on purpose — examples and fixes are
// documentation, not compiler behavior, so the core library stays lean.
internal sealed record DiagnosticDoc(
    DiagnosticDescriptor Descriptor,
    string Explanation,
    DiagnosticExample? Example = null);

// A compiler-verified example pair for a diagnostic: a minimal script that reports the code, and the
// corrected script that does not. DiagnosticDocsTests compiles both, so they cannot drift from what
// the compiler actually reports. BrokenHighlights/FixedHighlights name the substrings the page marks
// (the offending token in red, the correction in green); the examples themselves stay plain source.
internal sealed record DiagnosticExample(
    string Broken,
    string Fixed,
    IReadOnlyList<string> BrokenHighlights,
    IReadOnlyList<string> FixedHighlights,
    string? FixWhen = null,
    DiagnosticAlternativeFix? Alternative = null);

// A second, equally correct way out of a diagnostic, for the cases where the right edit depends on
// what the writer meant. Each fix says When it applies, so the page never implies one is the
// default. Like Fixed, it is compiled and must no longer report the code.
internal sealed record DiagnosticAlternativeFix(
    string When, string Fixed, IReadOnlyList<string> Highlights);
