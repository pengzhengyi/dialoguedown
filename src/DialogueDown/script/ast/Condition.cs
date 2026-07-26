using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A game-state condition: a query key the runtime reads as a boolean. Written
/// <c>`"key"?`</c> in a script, it guards a jump so the jump fires only when the condition is
/// true. It is a spanned inline fragment — the reusable primitive a jump (and, later, a line
/// or choice) guards — so tooling can point at the exact condition code span.
/// </summary>
internal sealed record Condition(string Key, SourceSpan Span) : InlineFragment(Span);
