using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A weight computed from game state, written as a query code span ending in a percent sign
/// (<c>`"Bob's Affection"%`</c>). The compiler preserves the query <see cref="Key"/> and its
/// source span; the runtime resolves the key to a number that becomes the option's weight. A
/// group containing a query weight defers its total checks to runtime, where the value is known.
/// </summary>
internal sealed record QueryWeight(string Key, SourceSpan Span) : ChoiceWeight(Span);
