using CsCheck;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Generates DialogueDown scripts for property tests.
/// </summary>
/// <remarks>
/// The generator aims at scripts a writer could plausibly have written rather than arbitrary
/// text: a property that only ever sees random characters exercises the front end's rejection
/// path and little else, so it would pass while saying nothing about the constructs the compiler
/// actually models. Each piece here is a construct the language defines, assembled in the
/// combinations a script puts them in.
/// <para>
/// A script is generated from its headings outwards, because the two things that decide whether a
/// script compiles are both properties of the whole document rather than of any one line: headings
/// must be distinct, or they claim the same anchor, and a jump must name a heading the script
/// contains. Generating jump targets beside the scenes instead of from them leaves most scripts
/// rejected in semantic analysis, and everything after that stage — resolution, lowering, the
/// graph — unreached and therefore untested.
/// </para>
/// <para>
/// Generated content is deliberately small and drawn from a fixed vocabulary. A counterexample is
/// only useful if a person can read it, and a shrunk script of a few lines is a bug report where
/// four hundred characters of noise is not.
/// </para>
/// </remarks>
internal static class ScriptGen
{
    private static readonly string[] _vocabulary =
        ["dawn", "map", "gate", "coin", "road", "ash", "bell"];

    private static readonly Gen<string> _word = Gen.OneOfConst(_vocabulary);

    private static readonly Gen<string> _speaker =
        Gen.OneOfConst("Alice", "Bob", "Guide", "Merchant");

    private static readonly Gen<string> _key =
        Gen.OneOfConst("HasMap", "FoundKey", "Alice.HasMap", "IsNight");

    private static readonly Gen<string> _prose =
        Gen.Select(_word, _word, (a, b) => $"{a} {b}");

    private static readonly Gen<string> _controlLine =
        Gen.OneOf(
            Gen.Select(_word, w => $"`(\"{w}\")`"),
            Gen.Select(_word, w => $"`GiveGold(\"{w}\")`"));

    private static readonly Gen<string> _randomChoice =
        Gen.Select(_prose, _prose, (a, b) => $"- `60%` {a}\n- `%` {b}");

    // Markdown the compiler does not model as dialogue. Its handling is configurable, and the two
    // handlings take different paths: an ignored construct is dropped, while a kept one is sliced
    // from the source by its span. Both are generated, because only the kept path does the slicing.
    private static readonly Gen<string> _unmodeled =
        Gen.OneOfConst(
            // Ignored by the default policy.
            "---",
            "| a | b |\n| --- | --- |\n| 1 | 2 |",
            "```\ncode\n```",
            // Kept, so their source text is sliced by span.
            "<div>aside</div>",
            "<https://example.com>",
            "Look: <https://example.com> and <span>more</span>.");

    // Headings come from a shuffle rather than a list of independent draws, so they are distinct:
    // two scenes under the same heading claim the same anchor, which is an error in its own right
    // and would stop the script before the stages under test.
    private static readonly Gen<string> _script =
        Gen.SelectMany(
            Gen.Bool,
            Gen.Shuffle(_vocabulary, 1, 3),
            (frontMatter, headings) =>
                Gen.Select(
                    Utterance(AnchorOf(headings)).List[1, 4].List[headings.Length, headings.Length],
                    bodies => Assemble(frontMatter, headings, bodies)));

    /// <summary>A whole script: one or more scenes, optionally preceded by front matter.</summary>
    public static Gen<string> Script() => _script;

    // "# The gate" slugs to "the-gate", so a heading the script contains yields an anchor a jump
    // can resolve against.
    private static Gen<string> AnchorOf(string[] headings) =>
        Gen.OneOfConst(Array.ConvertAll(headings, heading => $"#the-{heading}"));

    // Speech that exercises the inline surface: plain words, styling, a query, a game call, and
    // a link — each of which becomes a different fragment, and each of which carries its own span.
    private static Gen<string> Speech(Gen<string> anchor) =>
        Gen.OneOf(
            _prose,
            Gen.Select(_word, w => $"*{w}*"),
            Gen.Select(_word, w => $"**{w}**"),
            Gen.Select(_key, k => $"the `\"{k}\"` of it"),
            Gen.Select(_word, w => $"{w} `playSound(\"wind\")`"),
            Gen.Select(_word, _word, anchor, (a, b, target) => $"{a} [{b}]({target})"));

    private static Gen<string> Utterance(Gen<string> anchor)
    {
        var speech = Speech(anchor);
        var line = Gen.Select(_speaker, speech, (s, t) => $"{s}: {t}");
        var conditionalLine =
            Gen.Select(_key, _speaker, speech, (k, s, t) => $"`{k}?` {s}: {t}");
        var jump = Gen.Select(_word, anchor, (w, target) => $"=> [{w}]({target})");
        var conditionalJump =
            Gen.Select(_key, _word, anchor, (k, w, target) => $"`{k}?` => [{w}]({target})");
        var choice =
            Gen.Select(
                Gen.OneOf(_prose, jump),
                Gen.OneOf(_prose, Gen.Select(_key, k => $"`{k}?` later")),
                (a, b) => $"- {a}\n- {b}");
        var blockControl =
            Gen.Select(
                _key, line, line,
                (k, a, b) => $"> `if` `{k}?`\n>\n> {a}\n>\n> `else`\n>\n> {b}");

        return Gen.OneOf(
            line, line, line,
            conditionalLine, jump, conditionalJump, _controlLine,
            choice, _randomChoice, blockControl, _unmodeled);
    }

    private static string Assemble(
        bool frontMatter, string[] headings, IReadOnlyList<List<string>> bodies) =>
        (frontMatter ? "---\ntitle: A Script\n---\n\n" : string.Empty)
        + string.Join(
            "\n\n",
            headings.Select(
                (heading, i) => $"# The {heading}\n\n" + string.Join("\n\n", bodies[i])))
        + "\n";
}
