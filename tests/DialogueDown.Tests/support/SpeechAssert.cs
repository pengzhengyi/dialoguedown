using System.Collections.Immutable;
using DialogueDown.Playbook;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertions over the speech a playbook carries, so a test reads as what a line says rather
/// than as a chain of type checks.
/// </summary>
internal static class SpeechAssert
{
    /// <summary>The only fragment in the list, as the kind it was expected to be.</summary>
    /// <typeparam name="TFragment">The kind expected.</typeparam>
    /// <param name="fragments">The speech to look in.</param>
    /// <returns>The single fragment.</returns>
    public static TFragment AssertSingle<TFragment>(ImmutableArray<SpeechFragment> fragments)
        where TFragment : SpeechFragment =>
        Assert.IsType<TFragment>(Assert.Single(fragments));

    /// <summary>Asserts the fragment is exactly the given words, said plainly.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="words">What it should say.</param>
    public static void AssertSays(SpeechFragment fragment, string words) =>
        Assert.Equal(words, Assert.IsType<TextFragment>(fragment).Text);

    /// <summary>Asserts the speech is exactly the given words, said plainly.</summary>
    /// <param name="fragments">The speech to check.</param>
    /// <param name="words">What it should say.</param>
    public static void AssertSays(ImmutableArray<SpeechFragment> fragments, string words) =>
        AssertSays(Assert.Single(fragments), words);

    /// <summary>Asserts the fragment is styled as expected, and returns what it wraps.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="style">The style expected.</param>
    /// <returns>The fragments the style wraps.</returns>
    public static ImmutableArray<SpeechFragment> AssertStyled(
        SpeechFragment fragment, SpeechStyle style)
    {
        var styled = Assert.IsType<StyledTextFragment>(fragment);

        Assert.Equal(style, styled.Style);

        return styled.Children;
    }

    /// <summary>Asserts the fragment links to the given target, and returns its label.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="target">Where it should lead.</param>
    /// <returns>The label shown for the link.</returns>
    public static ImmutableArray<SpeechFragment> AssertLinksTo(
        SpeechFragment fragment, string target)
    {
        var link = Assert.IsType<LinkFragment>(fragment);

        Assert.Equal(target, link.Target);

        return link.Label;
    }

    /// <summary>Asserts the fragment shows the given image, and returns its alt text.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="source">The image expected.</param>
    /// <returns>The alt text, as speech.</returns>
    public static ImmutableArray<SpeechFragment> AssertShows(SpeechFragment fragment, string source)
    {
        var image = Assert.IsType<ImageFragment>(fragment);

        Assert.Equal(source, image.Source);

        return image.Alt;
    }

    /// <summary>Asserts the fragment is the tag expected, reserved or not.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="name">The tag's name.</param>
    /// <param name="value">What it was given, if anything.</param>
    /// <param name="reserved">Whether the name is one the language reserves.</param>
    public static void AssertTagged(
        SpeechFragment fragment, string name, string? value, bool reserved)
    {
        var tag = Assert.IsType<TagFragment>(fragment);

        Assert.Equal(name, tag.Name);
        Assert.Equal(value, tag.Value);
        Assert.Equal(reserved, tag.Reserved);
    }

    /// <summary>Asserts the fragment asks the game for the given key.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="key">The key expected.</param>
    public static void AssertQueries(SpeechFragment fragment, string key) =>
        Assert.Equal(key, Assert.IsType<QueryFragment>(fragment).Key);

    /// <summary>Asserts the fragment runs the given built-in action.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="action">The action expected.</param>
    public static void AssertCommands(SpeechFragment fragment, string action) =>
        Assert.Equal(action, Assert.IsType<DefaultCommandFragment>(fragment).Action);

    /// <summary>Asserts the fragment calls the game by name, with the given arguments.</summary>
    /// <param name="fragment">The fragment to check.</param>
    /// <param name="name">The call expected.</param>
    /// <param name="args">The arguments it should carry.</param>
    public static void AssertCalls(SpeechFragment fragment, string name, params string[] args)
    {
        var call = Assert.IsType<CustomCommandFragment>(fragment);

        Assert.Equal(name, call.Name);
        Assert.Equal(args, call.Args.ToArray());
    }
}
