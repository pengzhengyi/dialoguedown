using DialogueDown.Diagnostics;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Creates a desugarer with DialogueDown's built-in rules, in dependency order. Every
/// composition root that needs the desugar stage builds it here, so they all run the same
/// pipeline. A rule that reports takes the sink for the compilation being desugared, so the
/// desugarer is built once per compilation rather than once per process.
/// </summary>
internal static class DesugarerFactory
{
    public static Desugarer CreateDefault(IDiagnosticSink diagnostics) =>
        new(
        [
            new JumpAssemblyRule(diagnostics),
            new ControlLineRecognitionRule(),
            new DefaultSpeakerRule(),
        ]);
}
