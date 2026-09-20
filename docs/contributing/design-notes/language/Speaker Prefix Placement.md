# Speaker prefix placement

> [!NOTE]
> Status: **implemented**. A line's speaker prefix is recognized after a leading
> **game call**, so the shipped `visual-novel` example's intent —
> `` `ShowSprite("yuki", "shy")` Yuki @yuki #heroine: … `` — is honored.

The `visual-novel` example opens lines with presentation calls, with Yuki the
intended speaker: `` `ShowSprite("yuki", "shy")` Yuki @yuki #heroine: … ``.
`PeelSpeaker` reads only the first inline — the game call — so it bails and the whole
`Yuki @yuki #heroine:` prefix becomes speech (`speaker = null`, default-filled
downstream). This note lets the prefix stand after a leading run of game calls so the
example compiles as written.

<!-- We also need to consider how runtime will handle this, add an issue which will require experimentation to confirm whether a leading command is handled as default or not handled -->

## The rule

The peel scans for the **first inline that carries non-whitespace text** and stops
there; only preceding **code spans** are skipped, and the prefix is recognized only
at a plain, unescaped `TextInline`. Skipped inlines stay in the speech; only the
matched prefix text is removed — **at the matched index**, which
`RemoveSpeakerPrefix` (`LineBuilder.cs:134-146`) takes instead of hard-coding
`_remaining[0]`. The prefix is still parsed from the matched inline's `Text` at its
`ContentSpan.Start`, so spans stay exact.

Links, images, autolinks, raw HTML, entities, and emphasis all carry text, so the
scan stops at them and no prefix is recognized. A leading **condition** already
works — `PeelCondition` runs before `PeelSpeaker` (`LineBuilder.cs:46-47`) and
`ConditionReader.cs:45` reads a leading `` `"key"?` `` code span as the guard; a
leading command or query has no such reader, which is the gap this note closes.

## Why emphasis is the stopping point

The scan stops at the first text-bearing inline, styling included, so an emphasis
run before a colon is **not** skipped and `*Alice*: Hello` stays unattributed,
exactly as today. That preserves
[`DLG1107`](../diagnostics/Styled%20Speaker%20Prefix%20Diagnostic.md): the
detector is called on the remainder from the **matched index** — the point the
scan stopped at — so a styled prefix still warns, and it now warns after a
leading game call too (`` `Wave()` *Alice*: Hi ``), because the detector sees
past the skipped code spans. A styled run that is not a speaker prefix
(`` `Wave()` *the great*: hi ``) stays quiet, as does one that follows a
text-bearing inline which already failed the prefix parse.

## Tags after a call now diagnose

`` `Wave()` #tag: Hi. `` compiles silently today: the tag-only prefix never reaches
the speaker builder, so `#tag` becomes an ordinary `Tag` fragment and the line plays
in the default voice. Once the prefix is recognized, the builder sees `#tag:` and
reports **`DLG1101`** (`TagsWithoutSpeaker`) — the same error a tag-only prefix earns
at the start of a line. This is correct, not a regression: the prefix is now
visible, so its existing validation applies. The tag is dropped and the line
recovers to the default speaker with speech `Hi.`.

## Deliberately excluded

Escapes, raw HTML, and HTML entities are out of scope; their behavior does not
change. A leading escaped character still declines the prefix through the existing
`IsFirstCharacterEscaped` guard (`LineBuilder.cs:81-83`), now applied at the matched
index. Escape behavior is **specified** in the [Symbol escape](./Symbol%20Escape.md)
note (*Writer-facing behavior*, lines 102–109; *Error and boundary cases*, lines
257–262) and the writer-facing [Escaping a
speaker prefix](../../../guide/speakers-and-lines.md#escaping-a-speaker-prefix)
section; those stay the single source of truth.

## Consequence for the emitted speaker

With the prefix recognized, the line declares `Yuki @yuki #heroine`, so
`speakers[1]` changes from an auto-declared bare name to
`{ "id": "yuki", "name": "Yuki", "tags": [{ "name": "heroine" }] }`.
`visual-novel.verified.json` is regenerated: nodes **2, 7, 11, 23, 27, 35** move
their prefix out of speech into the speaker, choice nodes **6** and **10** lose the
embedded ` Yuki: ` from their option labels, and `speakers[1]` gains its id and tag.
The `gallery`, `highrise-fire`, and `rpg-quest` goldens are unaffected.

## Testability

| Case                                    | Expectation                                               |
| --------------------------------------- | --------------------------------------------------------- |
| `` `Wave()` Alice: Hi. ``               | speaker `Alice` (name reference); call stays in speech    |
| `` `Wave()` @alice: Hi. ``              | speaker `@alice` (id reference); call stays in speech     |
| `` `Wave()` Yuki @yuki #heroine: Hi. `` | `SpeakerDeclaration` with id + tags                       |
| `` `A()` `B()` Alice: Hi. ``            | two calls skipped; speaker `Alice`                        |
| `` `Wave()` Hello there. ``             | no prefix; speaker-less, call + text stay                 |
| `` `Wave()` *Alice*: Hi. ``             | speaker-less; **`DLG1107`** (detector sees past the call) |
| `` `Wave()` *the great*: hi. ``         | speaker-less; no `DLG1107` (styled run is not a prefix)   |
| `[x](url) Alice: Hi.` (`![]` too)       | speaker-less; a link or image stops the scan              |
| `` `Wave()` #tag: Hi. ``                | `DLG1101`; tag dropped; default speaker                   |
| `*Alice*: Hi.` (`**`, `~~` too)         | still speaker-less, still `DLG1107`                       |
| `Alice: Hi.`                            | unchanged                                                 |

A `LineBuilderTests` case covers each row; the golden assertion is that regenerating
`visual-novel.verified.json` produces the move above and leaves the other three
byte-identical.

## Proposed guide wording

`docs/guide/speakers-and-lines.md:24-26` states the current rule. Its production becomes:

```ebnf
TextLine = { GameCall , Whitespace } , [ Speaker , ":" ] , Speech ;
```

and the prose at [`## Speaker`](../../../guide/speakers-and-lines.md#speaker) gains:

> A line may open with one or more **game calls** — presentation such as
> `` `ShowSprite("yuki", "shy")` `` — before the speaker. The calls play in order,
> then the speaker names who talks: `` `ShowSprite("yuki", "shy")` Yuki: Hello. ``
> is Yuki speaking *Hello.*, not an unattributed line. Only game-call code spans may
> precede the speaker; a link, image, or styled name before the colon stays speech.

## Integration

The fix corrects each document that states or implies the current "leading text
only" rule: the [transpiler note](../core/Markdown%20to%20Dialogue%20AST%20Transpiler.md)
(`LineBuilder`'s responsibility row), the [conditional line note](./Conditional%20Line.md),
and the guide above. The [styled speaker prefix note](../diagnostics/Styled%20Speaker%20Prefix%20Diagnostic.md)
quotes the old guard; the [Symbol escape](./Symbol%20Escape.md) `LineBuilder` row
needs the same one-word touch ("leading text" → "matched text").
