using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A block of dialogue content — one of the pieces that make up a script or scene body: a
/// <see cref="Line"/>, a group of <see cref="Choices"/>, a <see cref="RandomChoices"/>, or a
/// <see cref="SceneHeading"/>. A body keeps its blocks in source order; headings are flat
/// markers, not containers. The name parallels the front-end's <c>MarkdownBlock</c>.
/// </summary>
internal abstract record ScriptBlock(SourceSpan Span) : ScriptNode(Span);
