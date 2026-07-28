namespace DialogueDown.Script.Desugar;

/// <summary>
/// Creates a desugarer with DialogueDown's built-in rules, in dependency order. Every
/// composition root that needs the desugar stage builds it here, so they all run the same
/// pipeline.
/// </summary>
internal static class DesugarerFactory
{
    public static Desugarer CreateDefault() =>
        new(
        [
            new JumpAssemblyRule(),
            new ControlLineRecognitionRule(),
            new DefaultSpeakerRule(),
        ]);
}
