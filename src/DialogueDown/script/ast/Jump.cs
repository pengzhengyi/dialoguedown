using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A jump to another point in the dialogue. <see cref="Target"/> is where it goes, kept
/// as a plain string that a later stage resolves; it may be empty (an unresolved target)
/// but never null. <see cref="Label"/> is the shown text as an ordered fragment sequence,
/// so it can carry <see cref="Tag"/>s and styling like any other speech.
/// </summary>
internal sealed record Jump : InlineFragment, IConditional
{
    public Jump(
        string target, IReadOnlyList<InlineFragment> label, SourceSpan span,
        Condition? condition = null)
        : base(span)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
        Label = label;
        Condition = condition;
    }

    public string Target { get; }

    public IReadOnlyList<InlineFragment> Label { get; }

    /// <summary>The condition that guards this jump, or null for an unconditional jump.</summary>
    public Condition? Condition { get; }
}
