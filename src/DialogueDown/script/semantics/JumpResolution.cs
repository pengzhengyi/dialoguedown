namespace DialogueDown.Script.Semantics;

/// <summary>
/// What a <c>Jump</c>'s target resolves to: a <see cref="SceneJump"/> for a local anchor that
/// names a scene, a <see cref="TerminalJump"/> for the reserved <c>#END</c>, a
/// <see cref="FileScopedJump"/> for a target that names a file, and an
/// <see cref="UnresolvedJump"/> for an empty target or a local anchor no scene has.
/// </summary>
internal abstract record JumpResolution;

/// <summary>A jump resolved to a scene in the same document.</summary>
internal sealed record SceneJump(Scene Scene) : JumpResolution;

/// <summary>
/// A jump whose target names a file, kept as its <see cref="File"/> and optional
/// <see cref="Anchor"/>. Resolving a file-scoped target — even one that names the current file
/// by path — needs cross-file support, so it is deferred rather than treated as an error.
/// </summary>
internal sealed record FileScopedJump(string File, string? Anchor) : JumpResolution;

/// <summary>
/// A jump that points nowhere: its target is empty, or names a local anchor no scene has.
/// </summary>
internal sealed record UnresolvedJump : JumpResolution;

/// <summary>
/// A jump to the reserved <c>#END</c> anchor: it resolves to the End sentinel and ends the
/// dialogue when reached. The anchor is uppercase and matched case-sensitively, so it
/// can never collide with a heading's anchor, which is always lowercased.
/// </summary>
internal sealed record TerminalJump : JumpResolution;
