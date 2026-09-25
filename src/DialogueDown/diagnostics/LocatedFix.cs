namespace DialogueDown.Diagnostics;

/// <summary>
/// A suggested repair in the located view: a writer-facing <see cref="Title"/> and the
/// <see cref="Edits"/> that apply it. Offsets are absolute, so a tool can index the source
/// directly; the editor projection converts them to a client-relative shape.
/// </summary>
public sealed record LocatedFix(string Title, IReadOnlyList<LocatedEdit> Edits);
