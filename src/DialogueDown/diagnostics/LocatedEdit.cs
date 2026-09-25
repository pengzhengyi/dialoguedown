namespace DialogueDown.Diagnostics;

/// <summary>
/// One text replacement in a <see cref="LocatedFix"/>: the half-open source range
/// <c>[<see cref="StartOffset"/>, <see cref="EndOffset"/>)</c> it replaces and the
/// <see cref="NewText"/> written in its place. An insertion is an empty range, which places the
/// text before the range's start.
/// </summary>
public sealed record LocatedEdit(int StartOffset, int EndOffset, string NewText);
