namespace DialogueDown.Script.Ast;

/// <summary>
/// A node an optional <see cref="Ast.Condition"/> can guard: a <see cref="Line"/>, a
/// <see cref="ControlLine"/>, a <see cref="Choice"/>, a <see cref="RandomOption"/>, a
/// <see cref="Branch"/>, or a <see cref="Jump"/>.
/// </summary>
internal interface IConditional
{
    /// <summary>The condition guarding this node, or null when it is unconditional.</summary>
    Condition? Condition { get; }
}
