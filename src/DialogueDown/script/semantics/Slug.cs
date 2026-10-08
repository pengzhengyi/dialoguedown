using System.Text.RegularExpressions;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Turns heading text into a GitHub-style anchor slug, matching the
/// <a href="https://github.com/Flet/github-slugger"><c>github-slugger</c></a> algorithm an
/// editor's GitHub-flavored Markdown tooling uses, so a jump target an editor completes from a
/// scene heading matches the compiler's slug. The rules: lowercase, drop the same punctuation
/// set (keeping letters, digits, underscores, and existing hyphens), and turn each space into a
/// hyphen, with no trimming and no merging of consecutive hyphens.
/// </summary>
/// <remarks>
/// The heading <c>The Old Mill's Gate!</c> gives <c>the-old-mills-gate</c>.
/// </remarks>
internal static class Slug
{
    // The exact character set github-slugger removes: general and supplemental punctuation
    // blocks, common ASCII punctuation, and the curly apostrophe — but not spaces, hyphens,
    // or underscores, which the slug keeps or turns into hyphens.
    private static readonly Regex _strippedPunctuation = new(
        "[\u2000-\u206F\u2E00-\u2E7F\\\\'!\"#$%&()*+,./:;<=>?@\\[\\]^`{|}~\u2019]",
        RegexOptions.Compiled);

    /// <summary>The slug for <paramref name="text"/>; empty when nothing sluggable remains.</summary>
    public static string From(string text) =>
        _strippedPunctuation.Replace(text.ToLowerInvariant(), string.Empty).Replace(' ', '-');
}
