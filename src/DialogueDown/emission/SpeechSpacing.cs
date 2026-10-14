using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Emission;

/// <summary>
/// Normalizes one speech list the way a Markdown preview shows it: spaces typed together become
/// one, a command or a tag takes no room, and no space stands before the first visible piece or
/// after the last. A space, a tab, and an encoded line break collapse to a space; a named space
/// such as U+00A0 is kept.
/// </summary>
/// <remarks>
/// <para>
/// A styled text and a link's label belong to the list around them, so the spaces on either side
/// of their edges meet. An image's alt text is a list of its own, which its caller normalizes.
/// </para>
/// <para>
/// Whether a space stays depends on both sides of it: a kept space before it makes it a duplicate,
/// and no visible piece after it makes it trailing. Reading from the start tells the first, but
/// the second is known only once the rest of the list has been read, since everything after a
/// space may be commands and tags. So two passes do the work. Collapsing reads forward, keeps the
/// first collapsible space of each run, and drops the rest along with any before the first visible
/// piece. Trimming reads backward and drops each space with no visible piece after it. Both pass
/// over a command or a tag, which is what lets the spaces on its two sides meet.
/// </para>
/// <para>
/// Each pass carries one flag. Collapsing's <c>afterSpace</c> says the last thing kept was a
/// collapsible space, and starts true. Trimming's <c>visibleAfter</c> says a visible piece comes
/// later in the list, and starts false. A command or a tag changes neither. For example:
/// </para>
/// <code>
/// original:        " Then... "   Wave()   " I will not argue. "   #mood
/// after collapse:  "Then... "    Wave()   "I will not argue. "    #mood
/// after trimming:  "Then... "    Wave()   "I will not argue."     #mood
/// </code>
/// <para>Collapsing, reading forward:</para>
/// <list type="bullet">
/// <item>
/// <c>" Then... "</c> becomes <c>"Then... "</c>, because its first space comes before any visible
/// piece: <c>afterSpace</c> is still true from the start.
/// </item>
/// <item>
/// <c>" I will not argue. "</c> becomes <c>"I will not argue. "</c>, because its first space follows
/// the space that ends <c>"Then... "</c>: <c>Wave()</c> between them is unseen, so
/// <c>afterSpace</c> is still true.
/// </item>
/// </list>
/// <para>Trimming, reading backward:</para>
/// <list type="bullet">
/// <item>
/// <c>"I will not argue. "</c> becomes <c>"I will not argue."</c>, because nothing visible follows
/// its last space: <c>#mood</c> after it is unseen, so <c>visibleAfter</c> is still false.
/// </item>
/// <item>
/// <c>"Then... "</c> keeps its space, because <c>"I will not argue."</c> follows it:
/// <c>visibleAfter</c> is true.
/// </item>
/// </list>
/// <para>
/// A third pass, Placing, moves a space kept right after a command or a tag past any more of them,
/// to just before the next visible piece. A host speaks the words between two commands as a part
/// of the line and skips a part that is only a space, so the space must sit with the words it
/// separates:
/// </para>
/// <code>
/// original:        "Hi."   Bow()   " "   Wave()   "Bye."
/// after placing:   "Hi."   Bow()   Wave()   " "   "Bye."
/// </code>
/// </remarks>
internal static class SpeechSpacing
{
    /// <summary>Returns the list as the playbook carries it, with typed spacing collapsed.</summary>
    /// <param name="fragments">One speech list, as mapped from the script.</param>
    /// <returns>The same list, normalized.</returns>
    public static ImmutableArray<SpeechFragment> Normalize(IReadOnlyList<SpeechFragment> fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        return Placing.Place(new Trimming().Trim(new Collapsing().Collapse(fragments)));
    }

    // Whether a fragment that holds no other speech shows to the player: an image or a query does,
    // a command or a tag does not. Text and line breaks are handled before this is asked. Every
    // kind is named, so a kind added to the format fails here rather than passing as unseen.
    private static bool IsVisiblePiece(SpeechFragment fragment) =>
        fragment switch
        {
            ImageFragment or QueryFragment => true,
            DefaultCommandFragment or CustomCommandFragment or TagFragment => false,
            _ => throw new NotSupportedException(
                $"No spacing rule is defined for {fragment.GetType().Name}."),
        };

    // A styled text must hold something, so one left with nothing to style is dropped. A link may
    // have an empty label, and stays, because its target still leads somewhere.
    private static bool TryRestyle(
        StyledTextFragment styled,
        ImmutableArray<SpeechFragment> children,
        [NotNullWhen(true)] out SpeechFragment? kept)
    {
        if (children.IsEmpty)
        {
            kept = null;
            return false;
        }
        else
        {
            kept = new StyledTextFragment(styled.Style, children);
            return true;
        }
    }

    // Moves a space kept right after a command or a tag past any more of them, to just before the
    // next visible piece. It runs last, on what Trimming kept, where every space left has a visible
    // piece after it, in the list or after the list, as words can follow a styled text.
    private static class Placing
    {
        public static ImmutableArray<SpeechFragment> Place(ImmutableArray<SpeechFragment> fragments)
        {
            var placed = ImmutableArray.CreateBuilder<SpeechFragment>(fragments.Length);

            // The space waiting for the next visible piece. Left between two commands, it would be
            // a part of the line that says nothing, which a host skips.
            SpeechFragment? held = null;
            var afterUnseen = false;
            foreach (var fragment in fragments.Select(Placed))
            {
                if (afterUnseen && IsLoneSpace(fragment))
                {
                    held = fragment;
                    continue;
                }

                afterUnseen = IsUnseen(fragment);
                if (held is not null && !afterUnseen)
                {
                    placed.Add(held);
                    held = null;
                }

                placed.Add(fragment);
            }

            // Nothing visible followed it in this list, so it goes at the end, the nearest it can
            // come to the words after the list.
            if (held is not null)
            {
                placed.Add(held);
            }

            return placed.ToImmutable();
        }

        // What Collapsing leaves of a text holding only collapsible spaces, or a soft line break.
        private static bool IsLoneSpace(SpeechFragment fragment) =>
            fragment is LineBreakFragment or TextFragment { Text: " " };

        private static bool IsUnseen(SpeechFragment fragment) =>
            fragment is not (TextFragment or LineBreakFragment or StyledTextFragment or LinkFragment)
            && !IsVisiblePiece(fragment);

        private static SpeechFragment Placed(SpeechFragment fragment) =>
            fragment switch
            {
                StyledTextFragment styled =>
                    new StyledTextFragment(styled.Style, Place(styled.Children)),
                LinkFragment link => new LinkFragment(link.Target, Place(link.Label)),
                _ => fragment,
            };
    }

    // Keeps the first collapsible space of each run, written as a space, and drops the rest along
    // with every one before the first visible piece. A soft line break counts as a collapsible space
    // and stays a break when kept.
    private sealed class Collapsing
    {
        // Whether the last thing kept was a collapsible space. It starts true, so a space before
        // the first visible piece is dropped.
        private bool _afterSpace = true;

        public ImmutableArray<SpeechFragment> Collapse(IEnumerable<SpeechFragment> fragments)
        {
            var collapsed = ImmutableArray.CreateBuilder<SpeechFragment>();
            foreach (var fragment in fragments)
            {
                if (TryKeep(fragment, out var kept))
                {
                    collapsed.Add(kept);
                }
            }

            return collapsed.ToImmutable();
        }

        // The set CSS collapses: a space, a tab, and the line breaks an entity can write into a
        // text. A named space such as U+00A0 is one the writer chose, so it never collapses.
        private static bool IsCollapsible(char character) => character is ' ' or '\t' or '\n' or '\r';

        // Returns false when nothing of the fragment is left to keep.
        private bool TryKeep(SpeechFragment fragment, [NotNullWhen(true)] out SpeechFragment? kept)
        {
            switch (fragment)
            {
                case TextFragment text:
                    return TryKeep(text, out kept);

                case LineBreakFragment lineBreak:
                    kept = _afterSpace ? null : lineBreak;
                    _afterSpace = true;
                    return kept is not null;

                case StyledTextFragment styled:
                    return TryRestyle(styled, Collapse(styled.Children), out kept);

                case LinkFragment link:
                    kept = new LinkFragment(link.Target, Collapse(link.Label));
                    return true;

                // Every other kind holds no speech; IsVisiblePiece names each and rejects the rest.
                case var piece:
                    if (IsVisiblePiece(piece))
                    {
                        _afterSpace = false;
                    }

                    kept = piece;
                    return true;
            }
        }

        private bool TryKeep(TextFragment text, [NotNullWhen(true)] out SpeechFragment? kept)
        {
            var builder = new StringBuilder(text.Text.Length);
            foreach (var character in text.Text)
            {
                var isSpace = IsCollapsible(character);
                if (isSpace && _afterSpace)
                {
                    continue;
                }

                builder.Append(isSpace ? ' ' : character);
                _afterSpace = isSpace;
            }

            var collapsed = builder.ToString();
            if (collapsed.Length == 0)
            {
                kept = null;
                return false;
            }

            kept = collapsed == text.Text ? text : new TextFragment(collapsed);
            return true;
        }
    }

    // Drops every space with no visible piece after it, reading from the end. It runs on what
    // Collapsing kept, where every collapsible space left is a space or a soft line break.
    private sealed class Trimming
    {
        // Whether a visible piece comes later in the list.
        private bool _visibleAfter;

        public ImmutableArray<SpeechFragment> Trim(ImmutableArray<SpeechFragment> fragments)
        {
            // Pushed from the end, so the stack lists them from the start.
            var trimmed = new Stack<SpeechFragment>(fragments.Length);
            for (var index = fragments.Length - 1; index >= 0; index--)
            {
                if (TryKeep(fragments[index], out var kept))
                {
                    trimmed.Push(kept);
                }
            }

            return [.. trimmed];
        }

        // Returns false when nothing of the fragment is left to keep.
        private bool TryKeep(SpeechFragment fragment, [NotNullWhen(true)] out SpeechFragment? kept)
        {
            switch (fragment)
            {
                case TextFragment text:
                    return TryKeep(text, out kept);

                case LineBreakFragment lineBreak:
                    kept = _visibleAfter ? lineBreak : null;
                    return kept is not null;

                case StyledTextFragment styled:
                    return TryRestyle(styled, Trim(styled.Children), out kept);

                case LinkFragment link:
                    kept = new LinkFragment(link.Target, Trim(link.Label));
                    return true;

                // Every other kind holds no speech; IsVisiblePiece names each and rejects the rest.
                case var piece:
                    if (IsVisiblePiece(piece))
                    {
                        _visibleAfter = true;
                    }

                    kept = piece;
                    return true;
            }
        }

        private bool TryKeep(TextFragment text, [NotNullWhen(true)] out SpeechFragment? kept)
        {
            var trimmed = _visibleAfter ? text.Text : text.Text.TrimEnd(' ');
            if (trimmed.Length == 0)
            {
                kept = null;
                return false;
            }

            // Whatever is left ends in a visible piece, or already had one after it.
            _visibleAfter = true;
            kept = trimmed.Length == text.Text.Length ? text : new TextFragment(trimmed);
            return true;
        }
    }
}
