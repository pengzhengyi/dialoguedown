namespace DialogueDown.Configuration;

/// <summary>
/// The options one compilation session reads, taken from <see cref="CompilerOptions"/>: how far a
/// compile proceeds after an error, and which diagnostic codes it marks disabled.
/// </summary>
/// <param name="Mode">How far a compile proceeds after an error.</param>
/// <param name="DisabledDiagnostics">The upper-case codes whose diagnostics are marked disabled.</param>
internal sealed record CompilationSessionOptions(CompilationMode Mode, IReadOnlySet<string> DisabledDiagnostics);
