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
/// Generated content is deliberately small and drawn from a fixed vocabulary. A counterexample is
/// only useful if a person can read it, and a shrunk script of a few lines is a bug report where
/// four hundred characters of noise is not.
/// </para>
/// </remarks>
internal static class ScriptGen
{
    private static readonly Gen<string> _word =
        Gen.OneOfConst("dawn", "map", "gate", "coin", "road", "ash", "bell");

    private static readonly Gen<string> _speaker =
        Gen.OneOfConst("Alice", "Bob", "Guide", "Merchant");

    private static readonly Gen<string> _key =
        Gen.OneOfConst("HasMap", "FoundKey", "Alice.HasMap", "IsNight");

    private static readonly Gen<string> _prose =
        Gen.Select(_word, _word, (a, b) => $"{a} {b}");

    // Speech that exercises the inline surface: plain words, styling, a query, a game call, and
    // a link — each of which becomes a different fragment, and each of which carries its own span.
    private static readonly Gen<string> _speech =
        Gen.OneOf(
            _prose,
            Gen.Select(_word, w => $"*{w}*"),
            Gen.Select(_word, w => $"**{w}**"),
            Gen.Select(_key, k => $"the `\"{k}\"` of it"),
            Gen.Select(_word, w => $"{w} `playSound(\"wind\")`"),
            Gen.Select(_word, _word, (a, b) => $"{a} [{b}](#a-scene)"));

    private static readonly Gen<string> _line =
        Gen.Select(_speaker, _speech, (s, t) => $"{s}: {t}");

    private static readonly Gen<string> _conditionalLine =
        Gen.Select(_key, _speaker, _speech, (k, s, t) => $"`{k}?` {s}: {t}");

    private static readonly Gen<string> _jump =
        Gen.Select(_word, w => $"=> [{w}](#a-scene)");

    private static readonly Gen<string> _conditionalJump =
        Gen.Select(_key, _word, (k, w) => $"`{k}?` => [{w}](#a-scene)");

    private static readonly Gen<string> _controlLine =
        Gen.OneOf(
            Gen.Select(_word, w => $"`(\"{w}\")`"),
            Gen.Select(_word, w => $"`GiveGold(\"{w}\")`"));

    private static readonly Gen<string> _choice =
        Gen.Select(
            Gen.OneOf(_prose, Gen.Select(_word, w => $"=> [{w}](#a-scene)")),
            Gen.OneOf(_prose, Gen.Select(_key, k => $"`{k}?` later")),
            (a, b) => $"- {a}\n- {b}");

    private static readonly Gen<string> _randomChoice =
        Gen.Select(_prose, _prose, (a, b) => $"- `60%` {a}\n- `%` {b}");

    private static readonly Gen<string> _blockControl =
        Gen.Select(
            _key, _line, _line,
            (k, a, b) => $"> `if` `{k}?`\n>\n> {a}\n>\n> `else`\n>\n> {b}");

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

    private static readonly Gen<string> _utterance =
        Gen.OneOf(
            _line, _line, _line,
            _conditionalLine, _jump, _conditionalJump, _controlLine,
            _choice, _randomChoice, _blockControl, _unmodeled);

    private static readonly Gen<string> _scene =
        Gen.Select(
            _word,
            _utterance.List[1, 4],
            (heading, body) => $"# The {heading}\n\n" + string.Join("\n\n", body));

    private static readonly Gen<string> _script =
        Gen.Select(
            Gen.Bool,
            _scene.List[1, 3],
            (frontMatter, scenes) =>
                (frontMatter ? "---\ntitle: A Script\n---\n\n" : string.Empty)
                + string.Join("\n\n", scenes)
                + "\n");

    /// <summary>A whole script: one or more scenes, optionally preceded by front matter.</summary>
    public static Gen<string> Script() => _script;
}
