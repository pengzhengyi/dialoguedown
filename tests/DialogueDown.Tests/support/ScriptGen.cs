using CsCheck;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Generates DialogueDown scripts for property tests.
/// </summary>
/// <remarks>
/// Each piece is a construct the language defines, so a generated script reaches the stages past
/// the front end instead of being rejected as random text.
/// <para>
/// A script is generated from its headings outward: the headings are distinct, and every jump names
/// one of them, so most scripts pass semantic analysis and go on to lowering and the graph.
/// </para>
/// <para>
/// Content is small and drawn from a fixed vocabulary, so a shrunk counterexample stays readable.
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
        Gen.Select(
            _prose,
            _prose,
            (a, b) => $"""
                - `60%` {a}
                - `%` {b}
                """);

    // Markdown the compiler does not model as dialogue. Its handling is configurable, and the two
    // handlings take different paths: an ignored construct is dropped, while a kept one is sliced
    // from the source by its span. Both are generated, because only the kept path does the slicing.
    private static readonly Gen<string> _unmodeled =
        Gen.OneOfConst(
            // Ignored by the default policy.
            "---",
            """
            | a | b |
            | --- | --- |
            | 1 | 2 |
            """,
            """
            ```
            code
            ```
            """,
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

    // Speech that covers each inline construct: plain words, styling, a query, a game call, and a
    // link. Each becomes a different fragment with its own span.
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
                (a, b) => $"""
                    - {a}
                    - {b}
                    """);
        var blockControl =
            Gen.Select(
                _key, line, line,
                (k, a, b) => $"""
                    > `if` `{k}?`
                    >
                    > {a}
                    >
                    > `else`
                    >
                    > {b}
                    """);

        // A line can carry its own jump, so its way out is a divert rather than a succession; when
        // the line is also gated, the succession comes back as the fallthrough.
        var lineToDivert =
            Gen.Select(
                _speaker, speech, _word, anchor,
                (s, t, w, target) => $"{s}: {t}. => [{w}]({target})");
        var conditionalLineToDivert =
            Gen.Select(
                _key, _speaker, speech, _word, anchor,
                (k, s, t, w, target) => $"`{k}?` {s}: {t}. => [{w}]({target})");

        // A choice whose options are all gated, and an `if` with no `else`: both gain a succession
        // taken when every arm is declined.
        var gatedChoice =
            Gen.Select(
                _key, _key, _prose, _prose,
                (k1, k2, a, b) => $"""
                    - `{k1}?` {a}
                    - `{k2}?` {b}
                    """);
        var blockControlWithoutElse =
            Gen.Select(
                _key,
                line,
                (k, a) => $"""
                    > `if` `{k}?`
                    >
                    > {a}
                    """);

        return Gen.OneOf(
            line, line, line,
            conditionalLine, jump, conditionalJump, _controlLine,
            choice, _randomChoice, blockControl, _unmodeled,
            lineToDivert, conditionalLineToDivert, gatedChoice, blockControlWithoutElse);
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
