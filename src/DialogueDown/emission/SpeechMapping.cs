using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Emission;

/// <summary>
/// Writes what a line says: the AST's inline fragments as a playbook's speech, with extra spaces
/// collapsed so the playbook reads the way a Markdown preview shows it.
/// </summary>
/// <remarks>
/// <para>
/// Nothing is flattened to a string, because a host re-renders it: Godot as BBCode, the report
/// as HTML, a terminal as ANSI. Styles, links, and image labels therefore stay nested, and tags
/// stay where in the line they attached rather than being hoisted beside it.
/// </para>
/// <para>
/// Spacing is collapsed once for each speech list: a line's speech, an option's or a jump's
/// label, and an image's alt text. A styled text and a link's label are part of the list around
/// them, so the spaces on either side of their edges meet; an image's alt text is shown on its own.
/// </para>
/// </remarks>
internal static class SpeechMapping
{
    /// <summary>Writes a whole line's speech, in order, with extra spaces collapsed.</summary>
    /// <param name="fragments">What the line says.</param>
    /// <returns>The same speech as a playbook carries it.</returns>
    public static ImmutableArray<SpeechFragment> Write(
        IReadOnlyList<Ast.InlineFragment> fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        return SpeechSpacing.Normalize(WriteEach(fragments));
    }

    /// <summary>Writes one fragment.</summary>
    /// <param name="fragment">The fragment to write.</param>
    /// <returns>The same fragment as a playbook carries it.</returns>
    public static SpeechFragment Write(Ast.InlineFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        AssertIsSpeakable(fragment);

        return fragment switch
        {
            Ast.Text text => new TextFragment(text.Content),
            Ast.StyledText styled =>
                new StyledTextFragment(Write(styled.Style), WriteEach(styled.Children)),
            Ast.Link link => new LinkFragment(link.Target, WriteEach(link.Label)),
            Ast.Image image => new ImageFragment(image.Source, Write(image.Alt)),
            Ast.LineBreak => new LineBreakFragment(),
            Ast.GameCall call => EffectMapping.Write(call),
            Ast.Tag tag => new TagFragment(tag.Name, tag.Value, tag is Ast.ReservedTag),

            _ => throw new NotSupportedException(
                $"No playbook fragment is defined for {fragment.GetType().Name}."),
        };
    }

    // Writes each fragment, leaving the spacing to the speech list that holds them.
    private static ImmutableArray<SpeechFragment> WriteEach(
        IReadOnlyList<Ast.InlineFragment> fragments) =>
        [.. fragments.Select(Write)];

    // Some inline fragments describe flow rather than what is said: a jump becomes an edge, a
    // condition becomes a guard, and the indicator is consumed pairing the two. None survives
    // into a graph, so finding one in a line's speech would mean reading it out to the player.
    private static void AssertIsSpeakable(Ast.InlineFragment fragment)
    {
        if (fragment is Ast.Condition or Ast.Jump or Ast.JumpIndicator)
        {
            throw new InvalidOperationException(
                $"A {fragment.GetType().Name} describes flow, not speech, and should have become "
                + "an edge or a guard before a graph was built.");
        }
    }

    private static SpeechStyle Write(Ast.SpeechStyle style) => style switch
    {
        Ast.SpeechStyle.Italic => SpeechStyle.Italic,
        Ast.SpeechStyle.Bold => SpeechStyle.Bold,
        Ast.SpeechStyle.Strikethrough => SpeechStyle.Strikethrough,

        // Written out rather than cast: the two enums are declared apart, and a cast would let a
        // reordering on either side change the wire format silently.
        _ => throw new NotSupportedException($"No playbook style is defined for {style}."),
    };
}
