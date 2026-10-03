namespace DialogueDown.Script.Ast;

/// <summary>
/// The root of the Dialogue AST: the whole script as an ordered <see cref="Body"/> of
/// blocks in source order, headings included as flat <see cref="SceneHeading"/> markers;
/// grouping them into scenes is a later stage's job. It is a plain container, not a spanned
/// <see cref="ScriptNode"/>; the nodes it holds carry the spans.
/// </summary>
internal sealed record ScriptDocument(IReadOnlyList<ScriptBlock> Body);
