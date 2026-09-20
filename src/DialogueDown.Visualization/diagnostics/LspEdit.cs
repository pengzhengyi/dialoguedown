namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// One text replacement in an <see cref="LspFix"/>, with offsets relative to the diagnostic's own
/// range start: 0 is the diagnostic's first character. The relative shape keeps a pushed payload
/// correct while the writer types, because the diagnostic's range is the position the editor keeps
/// remapping; the client shifts each edit by that mapped start.
/// </summary>
internal sealed record LspEdit(int Start, int End, string NewText);
