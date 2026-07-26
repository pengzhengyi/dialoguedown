using DialogueDown.Common;
using DialogueDown.Script.Ast;
using Pidgin;
using static DialogueDown.Script.Desugar.FragmentParsers;
using static Pidgin.Parser<DialogueDown.Script.Ast.InlineFragment>;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Folds a jump and the pieces around it into one <see cref="Jump"/>, over a single fragment
/// sequence. The rule is a small grammar — an optional guarding <see cref="Condition"/>, the
/// <c>=&gt;</c> <see cref="JumpIndicator"/>, and the <see cref="Link"/> that follows, across
/// same-line whitespace — expressed with the Pidgin parser combinators so the pattern reads
/// declaratively instead of as hand-rolled index tracking and backtracking. A
/// <see cref="JumpIndicator"/> with no link is dangling: it degrades to the characters
/// <c>=&gt;</c>. A <see cref="LineBreak"/> is not blank, so it stops the scan and keeps a jump
/// single-line. It works one level at a time; nested sequences are reached by the rewriter.
/// </summary>
internal static class JumpAssembler
{
    private static readonly Parser<InlineFragment, InlineFragment> _blank =
        Token(fragment => fragment.IsBlank());

    // [Condition] blank* — an optional guard that also absorbs the whitespace after it.
    private static readonly Parser<InlineFragment, Maybe<Condition>> _guard =
        OfType<Condition>().Before(_blank.Many()).Optional();

    // [Condition] blank* => blank* Link  →  Jump
    private static readonly Parser<InlineFragment, InlineFragment> _conditionalJump =
        Parser.Map(FoldJump, _guard, OfType<JumpIndicator>().Before(_blank.Many()), OfType<Link>());

    // A => with no link after it is not a jump, so it degrades to the characters "=>".
    private static readonly Parser<InlineFragment, InlineFragment> _danglingArrow =
        OfType<JumpIndicator>().Select(indicator => (InlineFragment)new Text("=>", indicator.Span));

    // Try the whole jump first (Try backtracks a guard consumed before a missing link); otherwise
    // degrade a lone arrow, otherwise pass the fragment through untouched.
    private static readonly Parser<InlineFragment, IEnumerable<InlineFragment>> _grammar =
        Parser.OneOf(Parser.Try(_conditionalJump), _danglingArrow, Any).Many();

    public static IReadOnlyList<InlineFragment> Assemble(IReadOnlyList<InlineFragment> fragments) =>
        _grammar.ParseOrThrow(fragments).ToList();

    private static InlineFragment FoldJump(Maybe<Condition> guard, JumpIndicator arrow, Link link)
    {
        var condition = guard.GetValueOrDefault();
        var from = condition?.Span ?? arrow.Span;
        return new Jump(link.Target, link.Label, SourceSpan.Covering(from, link.Span), condition);
    }
}
