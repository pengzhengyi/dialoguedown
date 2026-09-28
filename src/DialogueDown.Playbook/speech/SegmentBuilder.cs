using System.Collections.Immutable;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Speech under division: the segments closed so far, and the words gathered since the last one.
/// </summary>
/// <remarks>
/// Fragments are taken in the order they were written. Words gather until a command arrives, which
/// closes a segment around them, and <see cref="Freeze"/> closes whatever words are left over.
/// <para>
/// Emphasis is divided by a command written inside it. The children are taken by a builder of
/// their own, and each side comes back out emphasized again, so <c>*polished `Shine()` bright*</c>
/// keeps both halves bold rather than losing the styling where it was divided.
/// </para>
/// </remarks>
internal sealed class SegmentBuilder
{
    private readonly ImmutableArray<SpeechSegment>.Builder _closed =
        ImmutableArray.CreateBuilder<SpeechSegment>();

    private readonly ImmutableArray<SpeechFragment>.Builder _words =
        ImmutableArray.CreateBuilder<SpeechFragment>();

    /// <summary>Whether a command has divided this speech.</summary>
    public bool IsDivided => _closed.Count > 0;

    /// <summary>Takes a whole run of speech and hands back what it came to.</summary>
    /// <param name="speech">The speech to take.</param>
    /// <returns>A builder that has taken all of it.</returns>
    public static SegmentBuilder Of(ImmutableArray<SpeechFragment> speech)
    {
        var builder = new SegmentBuilder();
        builder.TakeAll(speech);

        return builder;
    }

    /// <summary>Takes a run of speech, in the order it was written.</summary>
    /// <param name="speech">The speech to take. An empty run leaves the builder as it was.</param>
    public void TakeAll(ImmutableArray<SpeechFragment> speech)
    {
        foreach (var fragment in speech.OrEmpty())
        {
            Take(fragment);
        }
    }

    /// <summary>Takes one fragment, which either joins the current words or closes them off.</summary>
    /// <param name="fragment">The fragment to take.</param>
    /// <exception cref="NotSupportedException">
    /// The fragment is of a kind this has never been taught to divide on or say.
    /// </exception>
    public void Take(SpeechFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);

        switch (fragment)
        {
            case DefaultCommandFragment or CustomCommandFragment:
                Close(fragment);
                break;

            case StyledTextFragment styled:
                TakeStyled(styled);
                break;

            // A link and an image carry speech of their own, but each is one thing: dividing a
            // link would put two links where the writer wrote one, and dividing an image would
            // draw the picture twice.
            case TextFragment or QueryFragment or LineBreakFragment or TagFragment
                or LinkFragment or ImageFragment:
                TakeWord(fragment);
                break;

            // Every kind is named above so that a kind added to the format arrives here as a
            // failure rather than quietly becoming another word.
            default:
                throw new NotSupportedException(
                    $"No segmentation is defined for {fragment.GetType().Name}.");
        }
    }

    /// <summary>Closes the words left over and reads out every segment.</summary>
    /// <returns>
    /// The segments, in written order. Always at least one, so speech that says nothing still
    /// reads as a single silent segment.
    /// </returns>
    public ImmutableArray<SpeechSegment> Freeze() =>
        // Speech ending in a command has already had its last segment closed by that command. The
        // emptiness check is what still gives speech saying nothing a segment to be nothing in.
        _words.Count > 0 || !IsDivided
            ? _closed.ToImmutable().Add(new SpeechSegment(_words.ToImmutable(), Command: null))
            : _closed.ToImmutable();

    // Joins the words being gathered, exactly as it stands.
    private void TakeWord(SpeechFragment fragment) => _words.Add(fragment);

    // The words gathered so far are said, and then this command is performed.
    private void Close(SpeechFragment command)
    {
        _closed.Add(new SpeechSegment(_words.ToImmutable(), command));
        _words.Clear();
    }

    private void TakeStyled(StyledTextFragment styled)
    {
        var inner = Of(styled.Children);
        if (!inner.IsDivided)
        {
            TakeWord(styled);
            return;
        }

        // The inner builder's segments are cut open into this one, so a division found inside the
        // emphasis becomes a division out here. Each side is emphasized again on its way across.
        foreach (var segment in inner._closed)
        {
            TakeStyledWords(styled.Style, segment.Words);

            // A closed segment always names the command that closed it.
            Close(segment.Command!);
        }

        // Whatever the inner builder never closed stays open here, so words after the last command
        // inside the emphasis join whatever follows the emphasis itself.
        TakeStyledWords(styled.Style, inner._words.ToImmutable());
    }

    // Styling with nothing inside it would show a reader an emphasis around no words at all, which
    // is what a command opening or closing an emphasized run would otherwise leave behind.
    private void TakeStyledWords(SpeechStyle style, ImmutableArray<SpeechFragment> children)
    {
        if (!children.IsEmpty)
        {
            TakeWord(new StyledTextFragment(style, children));
        }
    }
}
