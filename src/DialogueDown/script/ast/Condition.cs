using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A game-state condition: a query key the runtime reads as a boolean, written as a code span
/// ending in <c>?</c>, such as <c>`Alice.HasKey?`</c>. It guards the node that holds it — a jump,
/// a line, a control line, a choice or random option, or a control branch — which plays only
/// when the condition is true. It is a spanned inline fragment, so tooling can point at the exact
/// condition code span.
/// </summary>
internal sealed record Condition(string Key, SourceSpan Span) : InlineFragment(Span);
