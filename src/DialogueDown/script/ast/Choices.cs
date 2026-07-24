using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A group of options offered at a branch, each a <see cref="Choice"/>. When
/// <see cref="IsOrdered"/> is true the options must be presented in this order (an
/// ordered list in the source); otherwise a later stage may shuffle their display.
/// </summary>
internal sealed record Choices(
    bool IsOrdered, IReadOnlyList<Choice> Options, SourceSpan Span) : ChoiceGroup(Span);
