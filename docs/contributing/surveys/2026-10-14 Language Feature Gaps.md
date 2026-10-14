# Language feature gaps (2026-10-14)

> [!NOTE]
> Survey of **2026-10-14**, against `main` at
> [`8d25661`](https://github.com/pengzhengyi/dialoguedown/tree/8d25661). This is a
> dated snapshot. It is not updated as the language changes. A later survey starts
> from this one and records what changed; see [Language surveys](README.md).

Six features that dialogue projects need and that DialogueDown lacks or has only on
paper. Writers use most of them daily in Ink, Yarn Spinner and Godot Dialogue
Manager. Each one is scored for importance, helpfulness, Markdown fit and build
effort, and each proposal was shown to two fresh readers. The companion
survey, [Construct audit](2026-10-14%20Construct%20Audit.md), scores the
constructs that already exist.

## Table of contents

- [Method](#method)
- [The short answer](#the-short-answer)
- [How the competitors compare](#how-the-competitors-compare)
- [The recommended set](#the-recommended-set)
  - [1. Once-only options and content](#1-once-only-options-and-content)
  - [2. Who says the chosen option](#2-who-says-the-chosen-option)
  - [3. Negation in conditions](#3-negation-in-conditions)
  - [4. Returning detour](#4-returning-detour)
  - [5. Cross-file jumps](#5-cross-file-jumps)
  - [6. Line identity for localization](#6-line-identity-for-localization)
- [Fresh-reader check](#fresh-reader-check)
- [Already covered, so no new syntax](#already-covered-so-no-new-syntax)
- [Deliberately not recommended](#deliberately-not-recommended)
- [Sources](#sources)

## Method

The survey follows the comparative-research step of the repository's
[`add-dialogue-construct`](https://github.com/pengzhengyi/dialoguedown/blob/8d25661/.claude/skills/add-dialogue-construct/SKILL.md)
skill. It compares Ink and Yarn Spinner with closer analogs (Godot Dialogue
Manager, ChoiceScript and Ren'Py). Each proposed shape is judged on writer
usability, Markdown alignment and language safety, and each states how to write its
characters literally.

The baseline is the [script language guide](../../guide/script-language.md), the
[design notes](../design-notes/README.md), implemented and proposed, and the open
issues on the survey date. Two fresh readers then interpreted the proposed syntax
without explanation: a narrative writer who knows Markdown but not Ink or Yarn, and
a game developer who has shipped with Ink, Yarn Spinner and Godot Dialogue Manager.
Their findings are in [Fresh-reader check](#fresh-reader-check). Feature 1's
recommendation changed because of them.

Scores run from 1 (low) to 5 (high):

| Metric | Question it answers |
| --- | --- |
| Importance | How often does dialogue work need it? |
| Helpfulness | How much writing does it save, or how many bugs does it prevent? |
| Markdown fit | Does the proposed shape read as plain Markdown and preview sensibly? |
| Effort | The repository's size labels. S is one component with no open design question. M touches several stages or needs a short design pass. L is a new subsystem or several PRs. |

## The short answer

**Build once-only options first.** The hub conversation is a staple of game
dialogue: the player asks a list of questions, and each one disappears after it is
asked. Ink makes every choice once-only by default. DialogueDown can only do it
today by inventing a flag and a command per option.

The ranking sorts by importance, then helpfulness, then effort (smaller first).

| # | Feature | Importance | Helpfulness | Markdown fit | Effort | Status on the survey date |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Once-only options and content | 5 | 5 | 4 | M | Missing. Visit counts are ruled out of the runner by design. |
| 2 | Who says the chosen option | 5 | 4 | 4 | M | Partial. A worded option is replayed by its own speaker, or by the default speaker when it names none. |
| 3 | Negation in conditions | 4 | 4 | 4 | M | Deferred by Conditions D10. The playbook can already carry it. |
| 4 | Returning detour | 4 | 4 | 3 | M | Concept only, in Progression Order. No syntax yet. |
| 5 | Cross-file jumps | 4 | 4 | 5 | L | Proposed. Parses and warns `DLG2016`, then the playbook drops the jump. |
| 6 | Line identity for localization | 4 | 3 | 4 | L | Deferred. The playbook has room for a `lineId`. |

## How the competitors compare

"Yes" means a dedicated construct. "Partial" means the job can be done with
something else, at some cost. Godot Dialogue Manager is included because
DialogueDown targets Godot, and it is the closest text-first rival there.

| Feature | DialogueDown | Ink | Yarn Spinner 3 | Godot Dialogue Manager | ChoiceScript | Ren'Py |
| --- | --- | --- | --- | --- | --- | --- |
| Once-only options | No | Default `*`, sticky `+` | Yes, `once` | Partial, `$>` and `[if]` | Yes, `*hide_reuse` | Yes, `menu` `set` |
| Once-only lines and blocks | No | Yes, read counts | Yes, `<<once>>` … `<<else>>` | Partial, variables | Partial, variables | Partial, variables |
| Option text not replayed | Partial, jump-only options | Opt-in, `[ ]` | Default | Default, unless the response names a speaker | Default | Default |
| Negation | No, inverse flags | Yes, `not` | Yes, `not` and `!` | Yes, GDScript | Yes, `not()` | Yes, Python |
| Returning jump | No, planned | Yes, tunnel `->->` | Yes, `<<detour>>` | Yes, `=><` | Yes, `*gosub` | Yes, `call` |
| Multi-file stories | Parsed only | Yes, `INCLUDE` | Yes, project | Yes, `import` | Yes, scenes | Yes, labels |
| Stable line IDs | No | No, none built in | Yes, `#line:`, auto-tagged | Yes, `[ID:]` | No | Yes, generated |
| Random pick among lines | Yes; `` `50%` `` and `` `%` `` play today, dynamic weights compile but the runner refuses them | Yes, `{~a\|b}` | Yes, line groups `=>` | Yes, `%`, `%3` | Partial, `*rand` | Partial, Python |
| In-script variables | No, by design: the host owns state | Yes, `VAR` | Yes, `<<set>>` | Yes, `set` | Yes, `*set` | Yes, `$` |

One false friend: Yarn Spinner 3 starts a *line group* (the engine picks one line)
with `=>`, while DialogueDown uses `=>` for a jump. A writer moving between the two
will misread both.

## The recommended set

### 1. Once-only options and content

Importance 5 · Helpfulness 5 · Markdown fit 4 · Effort M · Missing

**The writer problem.** A detective questions a witness. Each question should
vanish once asked, the menu should loop back until the player leaves, and when
nothing is left the scene should move on. Today every option needs a game-defined
flag, a command to set it and an inverse-flag condition, and the game must store all
of it. Even then, a spent option is offered as unavailable
([Conditions D9](../design-notes/language/Conditions.md#d9--a-player-option-is-shown-unavailable-not-removed)),
and the host decides whether to hide it.

```markdown
## Questioning

- `NotAskedAlibi?` Where were you at nine?

    `SetFlag("AskedAlibi")`

    Witness: At the opera.

    => [Questioning](#questioning)

- `NotAskedKnife?` Whose knife is this?

    `SetFlag("AskedKnife")`

    ...
```

**Proposed shape.** A leading `` `once` `` marker on the option, in the same slot
as a condition, and the same keyword as a block marker for lines:

```markdown
## Questioning

- `once` Where were you at nine?

    Witness: At the opera.

    => [Questioning](#questioning)

- `once` Whose knife is this?

    Witness: Never seen it.

    => [Questioning](#questioning)

- That will be all.
```

```markdown
> `once`
>
> Guard: Hail, traveler. I keep this gate.
>
> `else`
>
> Guard: Welcome back.
```

The block form mirrors Yarn's `<<once>>` block with its `<<else>>`. Yarn needs a
closing keyword; here the blockquote already ends the block. A bare
`` `once` `` code span is an error today (`DLG1102`), so the marker consumes no
valid text. A condition on a key named `once` keeps its `?` (`` `once?` ``), so the
two never collide.

When every option is spent, the menu is passed by and reading continues after the
list, as it already does for an all-unavailable menu. That gives Ink's fallback
choice for free, provided something follows the list; a menu with nothing after it
is refused as `leads-nowhere`.

| Shape | Verdict |
| --- | --- |
| `` - `once` Ask… `` | **Recommended.** Both fresh readers preferred it. One keyword covers options and blocks, and it stacks with a condition. It avoids the `##end` precedent below. |
| `- Ask… ##once` | The first draft of this survey. It fits the tag grammar, but readers saw it as inert metadata or a mistyped heading, and a tag at the end of a long option is easy to miss. [Progression Order](../design-notes/language/Progression%20Order.md) also rejected a `##end` terminator because reserved tags annotate speakers and lines and should not carry control flow. |
| Ink's `*` versus `+` bullets | Rejected. CommonMark accepts `-`, `*` and `+` as bullet markers but starts a *new* list when the marker changes, so a mixed menu would split in two. |

**Decisions this forces.**

- **Hidden or unavailable.** Runtime
  [D8](../design-notes/runtime/Dialogue%20Runtime%20Architecture.md#d8--a-menu-shows-unavailable-options)
  offers a false condition as unavailable and lets the host hide it, and Yarn marks
  a spent `once` option unavailable. A hub conversation wants spent options gone.
  Either follow D8 and rely on the host, or add the writer-level hide switch the
  construct audit proposes.
- **Where the state lives.** The runtime note says "Visit counts stay out: a host
  that wants 'only once' answers a query it owns." One way to honor that is to
  lower `` `once` `` to a generated condition and a generated command on a reserved
  key, so the host still stores the state and the runner stays stateless. Yarn takes
  a similar route: its `once` flag lives in the runner's variable storage, hidden
  from scripts. The other way is a seen-set in `PlayState`, next to the planned call
  stack, before save files freeze.
- **Identity across edits.** Both readers asked what keys the "already seen" state,
  and what happens to a player's save when the script is edited. A positional key
  re-offers or loses options after an edit. Effort M assumes a simple derived key; a
  durable identity makes this depend on feature 6, which is L.
- **Scope and order.** Per run or per save, what `` `once` `` means inside a nested
  menu, and its order against a condition and a weight (for example condition,
  then `` `once` ``, then weight) all need stating.

### 2. Who says the chosen option

Importance 5 · Helpfulness 4 · Markdown fit 4 · Effort M · Partial

**The writer problem.** Every menu raises this question. After the player picks a
worded option, DialogueDown replays it as a line. The speaker is the one the option
names (`- Bob: …`), or the *default speaker* when it names none. In the
[`rpg-quest`](https://github.com/pengzhengyi/dialoguedown/blob/8d25661/examples/rpg-quest.dialogue.md)
playbook, picking "Ask about the road to Cinderfell." produces a line by the default
speaker, which that script leaves anonymous (golden node 4, speaker 0). Yarn
Spinner, ChoiceScript and Ren'Py do not replay an option. Godot Dialogue Manager
replays a response only when it names a character. Ink replays it, and offers `[ ]`
to hide the part that should not be said.

**Proposed shape.** A reserved speaker tag `##player`, parallel to `##default`,
names who voices a chosen option that has no speaker of its own. A project switch in
`dialogue.toml` turns the replay off for games whose UI never repeats the choice. As
with `##default`, `\##player` writes the text literally.

```markdown
Hero @hero ##player: I should ask around.

- Ask about the road to Cinderfell. <!-- replayed as Hero, not the default speaker -->
```

Ink's mixed text (`"Hi[."]," I said`) is powerful but cryptic, and square brackets
with no URL are ordinary text in a Markdown preview. Leave it out.
`- => [Label](#target)` already covers a label that is never said.
[#587](https://github.com/pengzhengyi/dialoguedown/issues/587) tracks that
label-only feature, a separate decision from `##player`.

**Fresh-reader caution.** Both readers had low confidence here. Neither could tell
who voices an option with no speaker, and both asked whether `- Bob: Can I come with
you?` means the player speaks to Bob or Bob speaks. The option-speaker rule needs
teaching whichever shape ships.

### 3. Negation in conditions

Importance 4 · Helpfulness 4 · Markdown fit 4 · Effort M · Deferred

**The writer problem.** Today "unless it is raining" needs the game to define a
second key, `NotRainy`, for every flag a writer might want to negate. Feature 1's
workaround shows how quickly that doubles the game's vocabulary. Every competitor in
the table has a negation.

**Proposed shape.** A leading word `not` inside the condition span. It reads as
English, and the guide already prefers natural-phrase keys like
`` `Is Alice happy?` ``. Quoting the key keeps a literal name that starts with
"not ":

```markdown
`not Rainy?` => [Set off across the moor](#the-moor)

<!-- A key that really starts with "not " is quoted: -->
`"not ready"?` => [Wait outside](#outside)
```

Stop at `not`. Leave `and`, `or` and comparisons with the game, the way Yarn's
smart variables hide a computed condition behind a name such as `$can_build_chair`.
In DialogueDown the game exposes the same thing as one key, `CanBuildChair?`.
Nested conditional blocks already express "and". The runtime note's
[extension points](../design-notes/runtime/Dialogue%20Runtime%20Architecture.md#extension-points)
say a condition is an object with a `kind`, so `not` is additive and no existing
playbook breaks.
[D10](../design-notes/language/Conditions.md#d10--no-negation-no-expressions)
rejected `!` as cryptic for writers, and `not` avoids that objection. Ink's guide
gives a second reason: `!` can be mistaken for its once-only list syntax, so it
recommends `not`. Both fresh readers chose `not` over `!` and over inverse keys.

**Compatibility.** An unquoted key may contain spaces today, so a script that
already uses `` `not ready?` `` as one key would silently change meaning. D10 also
records "no `not`" as a decision, so this needs a note revision, not only a parser
change.

**Fresh-reader caution.** Both readers read `` `not Rainy?` `` correctly, and both
stumbled on the quoted escape. Elsewhere quotes mean "insert a value"
(`` `"Key"` ``), so `` `"not ready"?` `` read to them as "insert, then test". The
guide's [Quoting a key](../../guide/game-state.md#quoting-a-key) rule has to be
taught next to `not`. Case (`NOT`) and words that merely start with "not"
(`nothing?`) need a stated rule.

### 4. Returning detour

Importance 4 · Helpfulness 4 · Markdown fit 3 · Effort M · Concept only

**The writer problem.** A shop menu, a "tell me about yourself" exchange or a shared
farewell should be written once and visited from many scenes, coming back to where
the player was. Every competitor in the table has this.
[Progression Order](../design-notes/language/Progression%20Order.md) names the role
but leaves the spelling and the return boundary open.

**Proposed shape.** A two-way arrow, `<=>`, in the same slot as `=>`, followed by
the same Markdown link. It returns when the target scene's heading subtree is
exhausted, which the note names as the leading candidate.

```markdown
Guard: Anything else?

<=> [Browse the wares](#shop)

Guard: Safe travels.
```

| Shape | For | Against |
| --- | --- | --- |
| `<=>` | Reads as "there and back" and pairs visibly with `=>`. CommonMark does not open an HTML tag on `<=`, so it previews as text before a working link. `\<=>` writes it literally. The narrative writer preferred it. | It is the "spaceship" comparison operator in Ruby, PHP and Perl, and can read as "swap" or "two-way link". It contains `=>`, so the reader must match the longer arrow first; under the current rule it would most likely read as a stray `<` before an ordinary jump (inferred, not run). |
| `=><` (Godot Dialogue Manager) | A known precedent that reads in order, go then back. The game developer preferred it. | Looks like a typo of `=>`. Written with no space before a target, `=><shop` could open an HTML tag. |
| `=> [Shop ##return](#shop)` | No new punctuation. | The behavior hides inside the visible label. Both readers rejected it. |

Recommendation: `<=>`, with the split noted. The readers agreed on what matters more:
where a detour returns, whether a `=>` or `#END` inside the callee cancels the
return, recursion depth, and saving mid-detour must all be defined before the
spelling is final.

Cost: a call stack in `PlayState` (planned in the runtime note, not yet in the
code), a `detour` capability, and a return edge in the graph and the report. The
runtime note warns this must land before save files exist.

### 5. Cross-file jumps

Importance 4 · Helpfulness 4 · Markdown fit 5 · Effort L · Proposed

No new syntax is needed, and that is the point.
`=> [Meet Bob](chapter-02.dialogue.md#meet-bob)` is already a working link in every
Markdown viewer. The problem is that the playbook drops it today
([#465](https://github.com/pengzhengyi/dialoguedown/issues/465)). The compiler
warns with `DLG2016`, but a host that loads only the playbook sees nothing, so the
story plays differently from the script. Any game longer than a short scene
outgrows one file. This ranks fifth only because it is large and already designed in
[Cross-File Jump Resolution](../design-notes/language/Cross-File%20Jump%20Resolution.md),
not because it matters less. Schedule it before the first project grows past one
file.

### 6. Line identity for localization

Importance 4 · Helpfulness 3 · Markdown fit 4 · Effort L · Deferred

Writers do not ask for this, but every shipped, translated or voiced game needs it
([#464](https://github.com/pengzhengyi/dialoguedown/issues/464)). A node reference
is positional, so it changes whenever a line moves. Follow Yarn's model: a tool
writes a stable ID onto each line, so the writer need not type one, and the ID uses
the existing reserved tag-group grammar. Ren'Py derives translation IDs from the
preceding label and the statements in each unit, and Godot Dialogue Manager takes
hand-written `[ID:]` keys. `\##line` writes the tag literally.

```markdown
Guard: Halt! Who goes there? ##line=a91f3c
```

A `ddown` command adds missing IDs, the way the fix mode already edits source. The
same identity gives `` `once` `` (feature 1) something stable to key on. Both fresh
readers understood the tag. Their questions were about lifecycle: who writes it,
what happens to a duplicate after copy and paste, and what an edit does to it.

## Fresh-reader check

Two fresh readers interpreted the proposed syntax from a one-page primer of the
existing language, with no explanation of the new forms. One was a narrative writer
who knows Markdown but no other dialogue tool. The other was a game developer who
has shipped with Ink, Yarn Spinner and Godot Dialogue Manager. Both first saw the
`##once` draft of feature 1. Treat the results as design evidence, not as a user
study: two readers, both simulated.

| Proposal | Writer | Developer | What it raised |
| --- | --- | --- | --- |
| Once-only option | Medium | High on intent | `##once` read as inert metadata or a mistyped heading. Both preferred `` `once` ``. State scope and save identity. |
| `` `once` `` block | High | High | Without a `?`, `once` might be a game key. Whether `else` is optional. |
| `##player` | Low | Low | Who voices an option with no speaker, and what `Bob:` means inside an option. |
| `not` | High | High | The quoted escape `` `"not ready"?` `` reads as "insert, then test". |
| `<=>` | Medium-high | Medium | Spaceship operator and "swap" readings. Return boundary unstated. Split vote against `=><`. |
| `##line=` | Medium | High | Who writes it, and duplicates after copy and paste. |

Three problems came from both readers:

- **`##` is overloaded.** It starts a heading (`## Questioning`), marks a built-in
  (`##default`) and would carry behavior (`##once`), a role (`##player`) and an ID
  (`##line=`). One missing space changes a tag into a heading. This argues for
  keeping new reserved tags to metadata, which is why feature 1 moved to
  `` `once` ``.
- **A preview hides the logic.** A plain Markdown preview shows both branches of a
  `once` block, shows `<=>` as an ordinary link, and gives no hint that a condition
  is conditional. Proofreading behavior needs the report, not a preview.
- **The option-speaker rule is unclear.** This is not new syntax, but it surfaced
  through `##player`. The construct audit scores the existing option speaker.

## Already covered, so no new syntax

| Competitor feature | DialogueDown equivalent on the survey date |
| --- | --- |
| Barks and line groups | Random choice with static, auto and dynamic weights, plus conditional options that drop out of the pool. The compiler accepts all of these. The runner plays number and auto weights and refuses a key weight or a guarded random option (`unplayable-node`) until [Drawing a Random Option](../design-notes/runtime/Drawing%20a%20Random%20Option.md) is complete. |
| Gathers (rejoin after a choice) | Built in: after an option's body, reading continues after the list. |
| Pauses, typing speed, signals | A command in the middle of a line runs at that point in the sentence, for example `` `Wait(0.5)` ``. |
| Several entry points per file | The playbook's `anchors` list every scene a host may start at. `#START` is only a default and can wait. |
| Per-line metadata | Tags anywhere in speech. The construct audit flags that tags on the speaker prefix are global, which surprises writers. |
| Explicit end | `=> [The end](#END)`. |

## Deliberately not recommended

These are popular elsewhere. Each one either breaks the host-owns-state boundary
that keeps the playbook portable, or has no natural Markdown form.

- **In-script variables, `set`, arithmetic and comparisons.** Commands and queries
  already reach the game. A variable store means every runtime port must implement
  an expression language.
- **Inline text alternatives and inline conditional text** (Ink `{a|b}`, Godot
  Dialogue Manager `[[a|b]]` and `[if]…[/if]`). Braces and double brackets mean
  nothing in Markdown, and the compilation report already uses braces to show an
  unanswered query (`{key}`), so reusing them would confuse that display. Line-level
  random choice, conditional lines and `` `once` `` cover most uses. Ink's stopping
  sequences (first visit, second visit, then a last form) are the gap this leaves;
  `` `once` `` with `` `else` `` covers only two stages.
- **Node groups and saliency** (Yarn picks the best-fitting node from a group whose
  members carry `when:` conditions). Valuable for ambient systems, but a large
  design. Revisit once `` `once` `` and detours exist, since such groups build on
  both.
- **Functions, lists and threads** (Ink). These are programmer features. They do not
  suit a language meant for non-technical writers.
- **Styled text ranges** (Yarn `[wave]…[/wave]`). Markdown emphasis and the
  presentation layer already cover the common cases.

## Sources

- DialogueDown at `8d25661`: the [guide](../../guide/script-language.md); the design
  notes [Conditions](../design-notes/language/Conditions.md) (D9, D10),
  [Progression Order](../design-notes/language/Progression%20Order.md),
  [Random Choice](../design-notes/language/Random%20Choice.md),
  [Cross-File Jump Resolution](../design-notes/language/Cross-File%20Jump%20Resolution.md),
  [Dialogue Runtime Architecture](../design-notes/runtime/Dialogue%20Runtime%20Architecture.md)
  (D8, state, extension points) and
  [Offering a Choice](../design-notes/runtime/Offering%20a%20Choice.md); the
  [`rpg-quest` golden](https://github.com/pengzhengyi/dialoguedown/blob/8d25661/tests/DialogueDown.Tests/emission/goldens/rpg-quest.verified.json);
  issues [#59](https://github.com/pengzhengyi/dialoguedown/issues/59),
  [#464](https://github.com/pengzhengyi/dialoguedown/issues/464),
  [#465](https://github.com/pengzhengyi/dialoguedown/issues/465) and
  [#587](https://github.com/pengzhengyi/dialoguedown/issues/587).
- Ink: [Writing with Ink](https://github.com/inkle/ink/blob/master/Documentation/WritingWithInk.md)
  (once-only and sticky choices, fallback choices, `[ ]` suppression, alternatives,
  `not` versus `!`, tunnels, includes).
- Yarn Spinner 3: [YSDocs](https://github.com/YarnSpinnerTool/YSDocs), the pages on
  once, line groups, detour, saliency, smart variables, markup, tags and shadow
  lines.
- Godot Dialogue Manager:
  [Basic Dialogue](https://github.com/nathanhoad/godot_dialogue_manager/blob/main/docs/Basic_Dialogue.md),
  [Conditions and Mutations](https://github.com/nathanhoad/godot_dialogue_manager/blob/main/docs/Conditions_Mutations.md)
  and [Translations](https://github.com/nathanhoad/godot_dialogue_manager/blob/main/docs/Translations.md).
  The claim that a response replays as a line only when it names a character comes
  from an older copy of the docs and is not repeated in the current pages.
- ChoiceScript: commands checked against
  [`web/scene.js`](https://github.com/dfabulich/choicescript/blob/master/web/scene.js)
  (`hide_reuse`, `gosub`, `rand`, `goto_scene`, `not`).
- Ren'Py: [menus](https://github.com/renpy/renpy/blob/master/sphinx/source/menus.rst)
  (the `set` clause) and
  [translation](https://github.com/renpy/renpy/blob/master/sphinx/source/translation.rst)
  (generated IDs).
