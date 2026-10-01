namespace DialogueDown.Diagnostics;

/// <summary>
/// A suggested repair in the located view: a writer-facing <see cref="Title"/> and the
/// <see cref="Edits"/> that apply it. Offsets are absolute, so a tool can index the source
/// directly.
/// </summary>
public sealed record LocatedFix(string Title, IReadOnlyList<LocatedEdit> Edits);
