using System.Collections.Immutable;
using System.Text;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Emission;

/// <summary>
/// Normalizes one speech list the way a Markdown preview shows it: spaces typed together become
/// one, a command or a tag takes no room, and no space stands before the first visible piece or
/// after the last. A space, a tab, and an encoded line break collapse to a space; a named space
/// such as U+00A0 is kept.
/// </summary>
internal static class SpeechSpacing
{
    /// <summary>
    /// Returns the list as the playbook carries it, with typed spacing collapsed. A styled text
    /// and a link's label belong to the list around them.
    /// </summary>
    public static ImmutableArray<SpeechFragment> Normalize(IReadOnlyList<SpeechFragment> fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        return new Normalizer().Apply(fragments);
    }

    // One traversal of one list: a forward pass drops a collapsible space that follows a kept one,
    // then a backward pass drops one with no visible piece after it.
    private sealed class Normalizer
    {
        // Starts as though a collapsible space came before, so leading spaces are dropped.
        private bool _precededByCollapsible = true;
        private bool _followedByVisible;

        public ImmutableArray<SpeechFragment> Apply(IReadOnlyList<SpeechFragment> fragments) =>
            Backward(Forward(fragments));

        // The set CSS collapses: a space, a tab, and the line breaks an entity can write into a
        // text. A named space such as U+00A0 is one the writer chose, so it never collapses.
        private static bool IsCollapsible(char character) => character is ' ' or '\t' or '\n' or '\r';

        private ImmutableArray<SpeechFragment> Forward(IReadOnlyList<SpeechFragment> fragments)
        {
            var kept = ImmutableArray.CreateBuilder<SpeechFragment>();
            foreach (var fragment in fragments)
            {
                if (VisitForward(fragment) is { } next)
                {
                    kept.Add(next);
                }
            }

            return kept.ToImmutable();
        }

        private ImmutableArray<SpeechFragment> Backward(ImmutableArray<SpeechFragment> fragments)
        {
            var kept = new List<SpeechFragment>(fragments.Length);
            for (var index = fragments.Length - 1; index >= 0; index--)
            {
                if (VisitBackward(fragments[index]) is { } next)
                {
                    kept.Add(next);
                }
            }

            kept.Reverse();
            return [.. kept];
        }

        private SpeechFragment? VisitForward(SpeechFragment fragment)
        {
            switch (fragment)
            {
                case TextFragment text:
                    return ForwardText(text.Text);
                case LineBreakFragment:
                    return ForwardBreak();
                case StyledTextFragment styled:
                    {
                        var children = Forward(styled.Children);
                        return children.IsEmpty ? null : new StyledTextFragment(styled.Style, children);
                    }
                case LinkFragment link:
                    return new LinkFragment(link.Target, Forward(link.Label));
                case ImageFragment:
                case QueryFragment:
                    // A visible piece. An image's alt text is a list of its own, supplied already
                    // normalized by whoever mapped it.
                    _precededByCollapsible = false;
                    return fragment;
                default:
                    // A command or a tag: unseen, so it neither keeps nor clears a space.
                    return fragment;
            }
        }

        private SpeechFragment? VisitBackward(SpeechFragment fragment)
        {
            switch (fragment)
            {
                case TextFragment text:
                    return BackwardText(text.Text);
                case LineBreakFragment:
                    return _followedByVisible ? fragment : null;
                case StyledTextFragment styled:
                    {
                        var children = Backward(styled.Children);
                        return children.IsEmpty ? null : new StyledTextFragment(styled.Style, children);
                    }
                case LinkFragment link:
                    return new LinkFragment(link.Target, Backward(link.Label));
                case ImageFragment:
                case QueryFragment:
                    _followedByVisible = true;
                    return fragment;
                default:
                    return fragment;
            }
        }

        private SpeechFragment? ForwardText(string text)
        {
            var kept = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                if (IsCollapsible(character))
                {
                    if (_precededByCollapsible)
                    {
                        continue;
                    }

                    kept.Append(' ');
                    _precededByCollapsible = true;
                }
                else
                {
                    kept.Append(character);
                    _precededByCollapsible = false;
                }
            }

            return kept.Length == 0 ? null : new TextFragment(kept.ToString());
        }

        private SpeechFragment? ForwardBreak()
        {
            if (_precededByCollapsible)
            {
                return null;
            }

            _precededByCollapsible = true;
            return new LineBreakFragment();
        }

        private SpeechFragment? BackwardText(string text)
        {
            var kept = new char[text.Length];
            var length = 0;
            for (var index = text.Length - 1; index >= 0; index--)
            {
                var character = text[index];
                if (IsCollapsible(character))
                {
                    if (_followedByVisible)
                    {
                        kept[length++] = ' ';
                    }
                }
                else
                {
                    kept[length++] = character;
                    _followedByVisible = true;
                }
            }

            if (length == 0)
            {
                return null;
            }

            Array.Reverse(kept, 0, length);
            return new TextFragment(new string(kept, 0, length));
        }
    }
}
