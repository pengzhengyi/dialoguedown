using DialogueDown.Script.Ast;
using Pidgin;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Pidgin parser combinators over a stream of <see cref="InlineFragment"/>s — the shared toolkit
/// for the small grammars that fold inline fragments (jump assembly today; conditional lines and
/// choices later). Kept apart from a specific assembler so every fragment grammar reuses one
/// definition of "match a fragment of a given node kind".
/// </summary>
internal static class FragmentParsers
{
    /// <summary>
    /// Matches one fragment whose node kind is <typeparamref name="T"/>, narrowed to that type.
    /// It filters by type like LINQ's <c>OfType</c>: a fragment of another kind does not match.
    /// </summary>
    public static Parser<InlineFragment, T> OfType<T>()
        where T : InlineFragment =>
        Parser<InlineFragment>.Token(fragment => fragment is T).Select(fragment => (T)fragment);
}
