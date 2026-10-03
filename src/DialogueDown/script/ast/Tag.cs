using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Metadata attached to content: a <see cref="Name"/> and, for a tag group such as
/// <c>#mood=happy</c>, a <see cref="Value"/>. A <see cref="CustomTag"/> is project-defined; a
/// <see cref="ReservedTag"/> is built in and owned by DialogueDown.
/// </summary>
internal abstract record Tag(string Name, string? Value, SourceSpan Span) : InlineFragment(Span);
