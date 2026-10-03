using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// The <c>=&gt;</c> token that marks a jump. It is only the marker: a later stage
/// pairs it with the <see cref="Link"/> that follows to build the <see cref="Jump"/>.
/// </summary>
internal sealed record JumpIndicator(SourceSpan Span) : InlineFragment(Span);
