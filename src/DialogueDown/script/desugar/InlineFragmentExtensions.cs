using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Small queries over an <see cref="InlineFragment"/> shared by the desugar rules.
/// </summary>
internal static class InlineFragmentExtensions
{
    /// <summary>
    /// Whether the fragment is a run of plain text holding nothing but whitespace.
    /// A soft <see cref="LineBreak"/> is deliberately not blank: it ends a single-line
    /// construct such as a jump rather than padding it.
    /// </summary>
    public static bool IsBlank(this InlineFragment fragment) =>
        fragment is Text text && string.IsNullOrWhiteSpace(text.Content);

    /// <summary>Whether the fragment carries real content: the negation of <see cref="IsBlank"/>.</summary>
    public static bool NonBlank(this InlineFragment fragment) => !fragment.IsBlank();
}
