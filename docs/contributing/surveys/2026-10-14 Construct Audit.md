# Construct audit (2026-10-14)

> [!NOTE]
> Survey of **2026-10-14**, against `main` at
> [`8d25661`](https://github.com/pengzhengyi/dialoguedown/tree/8d25661). This is a
> dated snapshot. It is not updated as the language changes. A later survey starts
> from this one and records what changed; see [Language surveys](README.md).

The constructs of the script language on the survey date, each scored on how useful
and how intuitive it is, how natively it reads as Markdown, and how safe it is to
write. Each row ends in a verdict, so the audit can guide what to emphasize, extend,
simplify or remove. The companion survey,
[Language feature gaps](2026-10-14%20Language%20Feature%20Gaps.md), covers the
features that are missing.

## Table of contents

- [Method](#method)
- [What needs attention](#what-needs-attention)
  - [Nested bullets under an option open a second menu](#nested-bullets-under-an-option-open-a-second-menu)
  - [Two speakers on adjacent lines become one speech](#two-speakers-on-adjacent-lines-become-one-speech)
  - [A tag on the speaker prefix is global, not per line](#a-tag-on-the-speaker-prefix-is-global-not-per-line)
  - [A command or query that opens a line loses the speaker](#a-command-or-query-that-opens-a-line-loses-the-speaker)
- [Scorecard](#scorecard)
  - [Speech and speakers](#speech-and-speakers)
  - [Structure and flow](#structure-and-flow)
  - [Conditions and game state](#conditions-and-game-state)
  - [Document furniture](#document-furniture)
- [What to do with this](#what-to-do-with-this)
- [Caveats](#caveats)
- [Sources](#sources)

## Method

The audit reads the [script language guide](../../guide/script-language.md) and the
[design notes](../design-notes/README.md), the 31 diagnostics in
[Error codes](../../guide/error-codes.md), the five scripts in
[`examples/`](https://github.com/pengzhengyi/dialoguedown/tree/8d25661/examples),
and the emitted playbook goldens, which show what each example compiles to. Four of
the scripts have goldens (`gallery`, `highrise-fire`, `rpg-quest`, `visual-novel`).
The fifth, `diagnostics`, is broken on purpose and is left out of the counts. The
same competitors as the companion survey (Ink, Yarn Spinner, Godot Dialogue Manager,
ChoiceScript, Ren'Py) set the comparison.

Scores run from 1 (low) to 5 (high) on every axis:

| Metric | Question it answers |
| --- | --- |
| Useful | How often does a dialogue writer need it? |
| Intuitive | Does a fresh reader guess the meaning? |
| Markdown | Does it read and preview naturally as Markdown? |
| Safety | How hard is it to get wrong silently? (5 is hard.) |
| Diagnostics | The codes that guard it, as a rough measure of how many ways it can go wrong. "none" marks a known failure with no code behind it; "—" means there is nothing to catch. |

| Verdict | Meaning |
| --- | --- |
| Emphasize | Lead with it in the guide and the gallery. |
| Keep | Leave it as it is. |
| Extend | Add to the construct or its tooling. |
| Simplify | Shrink what the guide teaches or what the syntax allows. |
| Reconsider | The design may be wrong. |
| Fix | A defect, or a gap between the guide and the compiler. |

## What needs attention

Most of the language holds up well. Jumps as Markdown links, scenes as headings and
ignored author aids fit a Markdown reader better than the competitors' forms do.
Two of the four problems below sit where Markdown's own rules disagree with a
writer's screenplay habits. One is a design choice that reads wrongly. One is a
plain compiler bug. The first and fourth rest on the committed goldens, the second
on the guide and the transpiler, and the third on the guide and the source.

### Nested bullets under an option open a second menu

Verdict: **Fix**.

Nested bullets read as "then this happens". DialogueDown reads every nested bullet
as another *choice*, and the project's own examples use them the first way. Three
of the four compiled examples do this with two or more bullets, ten times in total,
and the goldens confirm the result. This is the first nested pair in
[`visual-novel`](https://github.com/pengzhengyi/dialoguedown/blob/8d25661/examples/visual-novel.dialogue.md):

```markdown
- I like the rain. And the company.
    - `ShowSprite("yuki", "warm")` Yuki: `("Yuki hides a smile behind her sleeve")`
      ...Then I will not argue.
    - => [Under One Umbrella](#under-one-umbrella)
```

In
[`visual-novel.verified.json`](https://github.com/pengzhengyi/dialoguedown/blob/8d25661/tests/DialogueDown.Tests/emission/goldens/visual-novel.verified.json)
node 6 is a *menu*. Its two options are Yuki's line and the jump, so the player is
asked to pick what Yuki says. The ten menus are `visual-novel` nodes 6, 10, 22, 26
and 30, `rpg-quest` nodes 5, 9 and 13, and `highrise-fire` nodes 7 and 24. Three
single-bullet cases (`visual-novel` nodes 38 and 41, `highrise-fire` node 38) make
the player pick from a menu of one.

**Recommendation.** Keep the rule, since it is consistent. Warn when a nested option
list contains a bare jump, or a line voiced by a named character next to other
items. Offer a quick fix that turns the bullets into indented paragraphs, through
the fix channel that `ddown compile --fix` already uses. Then correct the three
examples, and show the paragraph body form first in
[Choices](../../guide/structure-and-flow.md#choices), which has no example of it
yet.

### Two speakers on adjacent lines become one speech

Verdict: **Fix**.

Ink, Yarn Spinner and Godot Dialogue Manager treat every source line as a new line
of dialogue. In DialogueDown, Markdown's soft break keeps adjacent lines in one
speech
([Transpiler D3](../design-notes/core/Markdown%20to%20Dialogue%20AST%20Transpiler.md#d3--hard-breaks-split-lines-soft-breaks-stay)),
and the second line is not read as a speaker prefix. Alice's speech swallows Bob's
line, and nothing reports it. The guide's own example in
[Inline speaker declaration](../../guide/speakers-and-lines.md#inline-speaker-declaration)
is written this way:

```markdown
Alice @A #main: Hello, Bob!
Bob @B #npc: Hello, Alice!
Alice #avatar="alice.png": The weather is nice today!
```

By the guide's succession rules that is one speech by Alice, and none of the 31
codes catches it.

**Recommendation.** This is the cheapest high-value fix in the language. Warn when a
soft break is followed by something that parses as a speaker prefix, with a quick
fix that inserts the blank line. Expect some false positives on wrapped speech, such
as a continuation line that starts with `Note:`, so keep it a warning. Correct the
guide example.

### A tag on the speaker prefix is global, not per line

Verdict: **Reconsider**.

`@A #excited: I can't wait!` reads like "A, excitedly". It actually merges
`#excited` into speaker A for the whole project. Tags from later mentions pile up,
so `#mood=sad` after `#mood=happy` leaves the speaker with both
([`SpeakerSymbol.MergeTag`](https://github.com/pengzhengyi/dialoguedown/blob/8d25661/src/DialogueDown/script/semantics/SpeakerSymbol.cs)
keys on name and value). Ren'Py (`e happy "…"`), Dialogic portraits and Yarn line
tags all make mood per line. The `visual-novel` example explains the workaround in a
comment ("a speaker tag is global identity, not a per-line emotion") and uses
`ShowSprite` commands instead. The guide's claim that conflicting speaker metadata
is a compile-time error holds for names, IDs and `##default` only (`DLG2003` to
`DLG2006`).

**Recommendation.** Make tags on a prefix that points at a known speaker
(`Alice #happy:`, `@A #happy:`) apply to that line only. Keep project-wide identity
in `dialogue.toml` and in the first full declaration. This retires the partial
declaration, the lowest-scoring construct in the speech table.

### A command or query that opens a line loses the speaker

Verdict: **Fix**.

`` `ShowSprite("yuki", "warm")` Yuki: … `` is the main visual-novel idiom, and the
guide says it works: "The line still belongs to its speaker, even when a command
opens it" ([Commands](../../guide/game-state.md#commands)). The golden shows the
speaker read as text: node 7 has speaker 0 (the default) and the text `" Yuki: "` in
its speech. Six line nodes in `visual-novel` are affected (2, 7, 11, 23, 27 and 35).
This is [#485](https://github.com/pengzhengyi/dialoguedown/issues/485), and a fix
sits unmerged on the branch `fix/leading-code-span-speaker`.

## Scorecard

### Speech and speakers

| Construct | Useful | Intuitive | Markdown | Safety | Diagnostics | Verdict | Why |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Alice: Hello` | 5 | 5 | 4 | 4 | — | Emphasize | The screenplay form. Yarn Spinner and Godot Dialogue Manager write it the same way. Ink and ChoiceScript have no speaker syntax, and Ren'Py puts the name before a quoted string. |
| Default speaker, `##default` | 4 | 3 | 4 | 4 | 2006 | Keep | Narration without a prefix is natural. The `##` spelling is jargon, but there is only one such tag to learn. |
| Inline declaration `Alice @A #main:` | 3 | 2 | 3 | 3 | 2003–2005, 2007 | Simplify | Handy for prototypes. Point long projects to `dialogue.toml` as the primary place. |
| ID reference `@A:` | 2 | 3 | 4 | 4 | 2007 | Keep | Pays off once names are localized. Rarely needed before then. |
| Partial declaration `@A #tag:` | 2 | 1 | 3 | 2 | 2007 | Reconsider | Looks like per-line mood but changes the speaker globally. See above. |
| Whitespace around the colon | 3 | 5 | 5 | 5 | — | Keep | Forgiving, and matches what a preview shows. Whitespace elsewhere in speech is still being settled ([#489](https://github.com/pengzhengyi/dialoguedown/issues/489)). |
| Quoted speech `Alice: " Hi"` | 1 | 3 | 4 | 1 | none | Fix | Documented but not implemented: the quotes ship into the speech ([#490](https://github.com/pengzhengyi/dialoguedown/issues/490)). The issue plans to build it, after #489. |
| Escaping a speaker prefix | 1 | 2 | 4 | 3 | none | Keep | Correct, but a seven-row table for a rare case. Move it to a reference footnote. A warning is deferred ([#475](https://github.com/pengzhengyi/dialoguedown/issues/475)). |
| Styling `*`, `**`, `~~` | 4 | 5 | 5 | 4 | 1107 | Emphasize | Pure Markdown, and the presentation layer decides the rendering. |
| Tags `#tag`, `#g=v` | 4 | 4 | 3 | 3 | 1101 | Keep | Hashtags are familiar. A `#` at the start of a line is a heading, which the rules have to work around. |
| Reserved tags `##…` | 2 | 2 | 3 | 4 | 2008 | Keep | Only one exists. Fresh readers found `##` overloaded between headings, built-ins and proposed behavior, so new reserved tags should stay metadata (`##player`, `##line`), not control flow. |
| Images with alt-text tags | 3 | 4 | 5 | 4 | — | Keep | Previews as the real image. One known gap: an image inside a link label is flattened to its Markdown source ([#527](https://github.com/pengzhengyi/dialoguedown/issues/527)). |
| Backslash escapes | 3 | 4 | 5 | 4 | 1113 | Keep | The standard Markdown rule, extended consistently to the sigils. |

### Structure and flow

| Construct | Useful | Intuitive | Markdown | Safety | Diagnostics | Verdict | Why |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Scenes as headings | 5 | 5 | 5 | 4 | 2001, 2002, 2015 | Emphasize | Gives an outline, folding and anchors for free. Targets are case-sensitive, a known sharp edge. |
| Succession by blank line | 5 | 3 | 5 | 2 | none | Fix | Correct Markdown, but adjacent speaker lines merge silently. Add the diagnostic above. |
| Hard breaks (two trailing spaces, `\`) | 2 | 2 | 4 | 2 | none | Simplify | Trailing spaces are invisible, and editors strip them. Teach the blank line, and keep `\` as the only compact form. |
| Choices `-` | 5 | 4 | 5 | 4 | 2017 | Emphasize | A list of options is exactly what a bullet list looks like. |
| Option with a speaker `- Bob: Really?` | 3 | 2 | 5 | 4 | — | Extend | Useful when the player picks a line for a character, but both fresh readers asked whether the player speaks *to* Bob or Bob speaks. Teach the rule next to the first choice example. |
| Option body and nested menus | 5 | 2 | 3 | 2 | 3002 | Fix | Bullets read as steps but compile to a menu. Ten cases in the compiled examples. |
| Replaying a chosen option | 4 | 2 | 4 | 3 | — | Extend | Replayed by the option's speaker, or by the default speaker when it names none. See feature 2 of the companion survey. Label-only text is [#587](https://github.com/pengzhengyi/dialoguedown/issues/587). |
| Ordered `1.` versus unordered menus | 1 | 2 | 4 | 4 | — | Simplify | The runner always offers the written order; only a flag for the host differs. Keep the flag, but stop presenting it as a writer feature. |
| Random choice `` `50%` ``, `` `%` `` | 4 | 4 | 4 | 5 | 1104, 1105, 2010, 3003, 3004 | Emphasize | Pinning some odds and letting the rest split evenly has no direct counterpart in the compared tools. Thoroughly guarded. The runner plays number and auto weights. |
| Dynamic weights `` `key%` `` | 2 | 3 | 4 | 3 | runtime only | Keep | A niche, and its errors appear only at play time. The runner refuses it for now (`unplayable-node`). Used in three of the four compiled examples. |
| Jump `=> [x](#y)` | 5 | 4 | 5 | 4 | 1003, 1113, 2009, 2016 | Emphasize | The strongest Markdown fit in the language: a clickable link in any preview. |
| Jump-only option `- => [Label](#x)`, empty link text `=> [](#x)` | 3 | 3 | 4 | 4 | 2017 | Keep | The first gives a label-only option that is never replayed. The second saves writing the words twice when words already lead the option. Show both earlier in the guide. |
| `#END` | 4 | 4 | 3 | 5 | 2009 | Keep | Collision-free by design. In a preview the link goes nowhere, which is harmless. |
| Jump into another file | 4 | 5 | 5 | 1 | 2016 | Extend | Parses and warns, then the playbook drops the jump ([#465](https://github.com/pengzhengyi/dialoguedown/issues/465)). See feature 5 of the companion survey. |

### Conditions and game state

| Construct | Useful | Intuitive | Markdown | Safety | Diagnostics | Verdict | Why |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Condition `` `Key?` `` on a jump, line or option | 5 | 4 | 4 | 4 | 1106 | Emphasize | One rule in three places, and it reads like a question. Missing `not`; see feature 3 of the companion survey. |
| Unquoted keys | 4 | 4 | 4 | 4 | — | Keep | Natural phrases work, and quotes remain for the edge cases. Fresh readers misread a quoted key as a value read, so the escape needs teaching. |
| Unavailable option shown, not hidden | 4 | 3 | — | 4 | — | Extend | A sound default, but many games hide options. [Conditions D9](../design-notes/language/Conditions.md#d9--a-player-option-is-shown-unavailable-not-removed) leaves hiding to the host, and the runtime's extension points list hide versus disable. Letting the writer choose would also serve once-only options. |
| Conditional block ``> `if` `` | 4 | 2 | 2 | 2 | 1108–1112 | Extend | Previews as one grouped quote, which reads acceptably. The cost is the `>` discipline: every line needs it, a nested block ends only at a bare `>`, and five diagnostics exist for this construct alone. The report editor already continues `>` on Enter ([Block Controls](../design-notes/language/Block%20Controls.md)). Add quick fixes for the five diagnostics through the existing fix channel. |
| Value query `` `"Key"` `` | 4 | 3 | 4 | 4 | 1102 | Keep | Quotes are required here but optional everywhere else, the one inconsistency a writer will notice. |
| Query in a label | 3 | 4 | 4 | 4 | — | Keep | Works where a writer expects it to. |
| Custom command `` `Fn("a")` `` | 5 | 4 | 4 | 4 | 1102, 1103 | Emphasize | The workhorse of every example, with 36 uses across the four compiled scripts. |
| Default command `` `("text")` `` | 3 | 3 | 4 | 4 | 1102 | Keep | A useful free-form stage direction. The bare-parenthesis spelling takes some learning. |
| Command in mid-line | 4 | 4 | 4 | 4 | — | Emphasize | Covers pauses, typing speed and signals without new syntax. |
| Command or query before the speaker | 4 | 4 | 4 | 1 | none | Fix | The speaker is lost (#485). See above. |

### Document furniture

| Construct | Useful | Intuitive | Markdown | Safety | Diagnostics | Verdict | Why |
| --- | --- | --- | --- | --- | --- | --- | --- |
| HTML comments | 4 | 5 | 5 | 5 | — | Keep | Standard, and hidden in previews. |
| Front matter | 3 | 5 | 5 | 5 | — | Keep | A natural future home for per-script settings. |
| Author aids (tables, code, `---`, mermaid), ignored | 3 | 4 | 5 | 4 | 1114 | Emphasize | The compared languages offer comments only. A rendered cast table or flow diagram that stays in the script is a real differentiator. |

## What to do with this

| Action | Items |
| --- | --- |
| Fix first (small) | The soft-break speaker diagnostic, and the guide example. The nested-option diagnostic, and the three examples. Land #485 from its existing branch. Settle quoted speech (#490). |
| Reconsider | Make speaker-prefix tags per line, and retire the partial declaration. Stop teaching trailing-space hard breaks and ordered-versus-unordered menus as writer features. |
| Emphasize in the guide and gallery | Jumps as links, scenes as headings, author aids, the auto weight and mid-line commands. These are DialogueDown's real advantages and should lead the guide. |
| Extend | Teach the option-speaker rule. Let the writer choose to hide or disable an unavailable option. Add quick fixes for the conditional-block diagnostics. Keep new reserved tags to metadata. |

## Caveats

- **Judgement scores.** Intuitive and Markdown scores are judgement calls informed by
  the competitor comparison. Two simulated fresh readers informed the option-speaker,
  quoted-key and reserved-tag rows; nothing here is a user study.
- **What was not run.** The first and fourth findings come from the committed
  goldens. The second is inferred from the guide's succession rules, Transpiler D3,
  the hard-break splitting in `BlockBuilder.cs`, and the absence of a matching code
  in the diagnostic catalog. The third is read from `SpeakerSymbol.MergeTag` and the
  guide. Neither was run, because the survey environment had no .NET SDK.
- **Goldens are current.** The goldens are verified snapshot tests, so CI holds them
  equal to the compiler's output at `8d25661`.

## Sources

- DialogueDown at `8d25661`: the [guide](../../guide/script-language.md); the design
  notes
  [Markdown to Dialogue AST Transpiler](../design-notes/core/Markdown%20to%20Dialogue%20AST%20Transpiler.md)
  (D3, D6), [Block Controls](../design-notes/language/Block%20Controls.md),
  [Conditions](../design-notes/language/Conditions.md) (D9),
  [Progression Order](../design-notes/language/Progression%20Order.md),
  [Offering a Choice](../design-notes/runtime/Offering%20a%20Choice.md),
  [Dialogue Runtime Architecture](../design-notes/runtime/Dialogue%20Runtime%20Architecture.md)
  (D8, extension points) and
  [Diagnostic Quick Fixes](../design-notes/visualization/editor/Diagnostic%20Quick%20Fixes.md);
  the [emission goldens](https://github.com/pengzhengyi/dialoguedown/tree/8d25661/tests/DialogueDown.Tests/emission/goldens);
  and issues #475, #485, #489, #490, #527 and #587.
- Competitors: the
  [Ren'Py say statement](https://github.com/renpy/renpy/blob/master/sphinx/source/dialogue.rst)
  (image attributes), Yarn Spinner's
  [tags and metadata](https://github.com/YarnSpinnerTool/YSDocs), and the Ink, Godot
  Dialogue Manager and Yarn pages listed in the companion survey. The Dialogic
  portrait claim is from memory and was not re-checked.
- Fresh-reader results: the
  [fresh-reader check](2026-10-14%20Language%20Feature%20Gaps.md#fresh-reader-check)
  in the companion survey.
