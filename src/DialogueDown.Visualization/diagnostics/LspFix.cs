namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// A suggested repair for an <see cref="LspDiagnostic"/>: a writer-facing title and the edits that
/// apply it, both carried so the editor can offer and apply the fix without knowing the language.
/// </summary>
internal sealed record LspFix(string Title, IReadOnlyList<LspEdit> Edits);
