# Conditions

> [!NOTE]
> Status: **implemented**. The compiler recognizes and preserves the **condition**
> primitive (`` `"key"?` ``) and binds it to every construct that can be guarded.
> Evaluating a condition at play time — the `IGameSystem.Check` read — is part of
> the planned [runtime](https://github.com/pengzhengyi/dialoguedown/issues/45).

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Writer-facing behavior](#writer-facing-behavior)
- [Grammar](#grammar)
- [Where a condition may be written](#where-a-condition-may-be-written)
- [Condition resolution](#condition-resolution)
- [Prior art](#prior-art)
- [Key design decisions](#key-design-decisions)
  - [D1 — A condition is a query read, not a command](#d1--a-condition-is-a-query-read-not-a-command)
  - [D2 — The `?` sigil joins the query-and-sigil family](#d2--the--sigil-joins-the-query-and-sigil-family)
  - [D3 — Guard-first placement](#d3--guard-first-placement)
  - [D4 — A dedicated boolean read on `IGameSystem`](#d4--a-dedicated-boolean-read-on-igamesystem)
  - [D5 — A condition is a spanned, reusable node](#d5--a-condition-is-a-spanned-reusable-node)
  - [D6 — Negation is deferred](#d6--negation-is-deferred)
  - [D7 — No in-script expression language](#d7--no-in-script-expression-language)
- [Markdown interaction](#markdown-interaction)
- [Diagnostics](#diagnostics)
- [Deferred work](#deferred-work)

## Goal and scope

A writer often wants something to happen only under some game-state condition —
take a shortcut once a key is found, greet a returning player differently, offer
a menu option only to whoever is carrying the map.

This note owns the **condition** primitive itself: what it is, how it is written,
how it resolves, and the decisions that shape it. Each construct that can be
guarded has its own note covering where the guard attaches and what a false
condition does there — see
[Where a condition may be written](#where-a-condition-may-be-written).

Read this note first. A construct note assumes it and does not repeat it.

## Ubiquitous language

The domain term is **condition** everywhere: the AST node, the diagnostics, the
writer-facing specification, commits, and the changelog.

| Term            | Meaning                                                                                                                             |
| --------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| **Condition**   | A game-state query read as a boolean: `` `"key"?` `` — the query key, delimited by quotes, followed by a `?`.                       |
| **Guard**       | A condition attached to a construct, controlling whether that construct happens.                                                    |
| **Guard-first** | The condition is written *before* the thing it guards, so it reads "if … then …" and one placement rule serves every construct.     |
| **Check**       | The boolean the game answers for a condition's key, through `IGameSystem.Check`; an unknown key is `false`.                         |
| **Peel**        | Removing a leading guard code span from a block before the rest is parsed, so the condition becomes a property rather than content. |

## Writer-facing behavior

A condition is the game-state [query](../../guide/game-state.md#queries) you
already write, with a `?` added inside the code span:

```markdown
`"FoundKey"?` => [Open the vault](#the-vault)
```

It is the third member of the **query-and-sigil** family, so a writer who knows
queries and weights already knows its shape:

| Syntax         | Meaning                                     |
| -------------- | ------------------------------------------- |
| `` `"key"` ``  | Insert the query's value into speech.       |
| `` `"key"%` `` | Weight a random-choice option by the value. |
| `` `"key"?` `` | Read the value as a boolean condition.      |

Because the key is quoted, a key that itself contains a `?` is unambiguous — the
operator is the `?` after the closing quote. In `` `"Rainy?"?` `` the key is
`Rainy?`.

**Negation.** There is no `not` operator. To branch when a flag is *false*, query
a game-defined inverse: `` `"NotRainy"?` `` (see [D6](#d6--negation-is-deferred)).

**No inline else.** A condition guards exactly one thing, and a false condition
falls through to whatever comes next. The writer places the alternative on the
following line, or reaches for a
[block control](./Block%20Controls.md) when they want grouped, mutually exclusive
branches with a fallback.

## Grammar

A condition is a code span whose content is a quoted query key followed by `?`:

```ebnf
Condition = "`" , '"' , QueryKey , '"' , "?" , "`" ;
```

`QueryKey` is the same key a [query](../../guide/game-state.md#queries) uses,
recognized by the same grammar; a condition reuses that recognition rather than
re-deriving that a query is quoted.

## Where a condition may be written

One primitive, four guards. Each construct decides where the guard attaches and
what a false condition means there:

| Construct                                  | Attaches                             | When false                                                                                       |
| ------------------------------------------ | ------------------------------------ | ------------------------------------------------------------------------------------------------ |
| [Jump](./Conditional%20Jump.md)            | Inline fragment, bound in desugar    | The jump does not fire; reading continues with the next block.                                   |
| [Line](./Conditional%20Line.md)            | Peeled at the block start            | The line is skipped whole.                                                                       |
| [Choice option](./Conditional%20Choice.md) | Peeled at the list item              | A player option is removed; a random option is excluded and the remaining weights re-normalized. |
| [Block control](./Block%20Controls.md)     | The `` `if` ``/`` `elseif` `` marker | The branch is not taken; the next branch or `` `else` `` runs.                                   |

Guards attach at different points because the constructs differ: a jump can appear
mid-line, while a line guard and an option guard always sit at the start of their
block. Each note explains its own choice.

## Condition resolution

Resolution runs at **runtime**, not compile time — the game state a condition
reads is unknown until the game runs. The compiler only recognizes and preserves
the condition; the contract below is what the runtime honors, and it is the same
for every construct:

1. The runtime reads the key through `IGameSystem.Check`, which returns a boolean.
2. A `true` result lets the guarded construct happen; a `false` result applies
   that construct's false behavior from the table above.

An unknown key is the game's default `false`, so a flag that was never set simply
does not fire. Because the game answers with a real boolean, there is no string
parsing and no invalid-value case to report.

## Prior art

Two models dominate conditional flow in dialogue languages:

- **Inline guard** — Ink writes `{has_key: -> unlocked}` for a divert,
  `{angry: You again?}` for text, and `* {has_key} [Use the key]` for a choice: a
  condition immediately in front of what it gates, falling through when false.
  This is the model here — compact, inline, and guard-first. Ren'Py's
  `"Use the key" if has_key:` attaches a condition to a menu choice the same way.
- **Block `if`** — Yarn Spinner writes `<<if $c>> … <<endif>>` and Ren'Py writes
  `if c: …`. Powerful (`elseif`/`else`, multiple statements) but a block
  statement rather than a Markdown-native inline. DialogueDown offers this shape
  separately, as a [block control](./Block%20Controls.md).

The lasting lessons:

- **A condition reads; it does not act.** Ink, Yarn, and Ren'Py all keep a
  condition (a read) distinct from a command (a side effect). A DialogueDown
  condition is a read (`Check`), never a command.
- **Fall-through is the natural false behavior.** What "fall through" means
  depends on the construct — skip the jump, hide the line, drop the option — but
  in every case nothing else has to be written for the false case.
- **Expressions belong to the host, not the script.** Ren'Py leans on Python and
  Yarn/Ink on their own expression languages. DialogueDown delegates the logic to
  `IGameSystem`: the script names a boolean, the game computes it.

## Key design decisions

### D1 — A condition is a query read, not a command

A condition reads game state, so it belongs on the **read side** of `IGameSystem`
(resolved by `Check` — see [D4](#d4--a-dedicated-boolean-read-on-igamesystem)),
not the command lane (`Execute`, which performs side effects). Syntactically it
still reuses the quoted-query *form* the writer already knows.

A reserved command form such as `` `If("Rainy")` `` was rejected: it borrows the
command grammar (`Name(args)`) for a *read*, would have to reserve `If`/`Unless`
out of the game's command names, and tempts the in-script expression language
DialogueDown avoids.

### D2 — The `?` sigil joins the query-and-sigil family

DialogueDown already reads a query and applies a sigil: `` `"key"` `` inserts the
value and `` `"key"%` `` weights an option. `` `"key"?` `` reads the value as a
boolean — a consistent third member, with the family's escaping already solved
(the quotes delimit the key; the sigil follows the closing quote).

### D3 — Guard-first placement

The condition is written *before* what it guards. It reads "if … then …", it is
scannable at the start of the construct, and one placement rule serves the jump,
the line, the choice option, and the block control alike — matching Ink's
`{cond} …`.

Placing the condition *after* the construct reads naturally in English but hides
it at the line's end and does not generalize across the four guards.

### D4 — A dedicated boolean read on `IGameSystem`

A condition resolves through `bool Check(string key)` on `IGameSystem`, beside
`Query` (a value) and `Execute` (an effect). The game returns a real boolean, so
the runtime never parses a string into a truth value and there is no truthiness
ladder — an unknown key is simply the game's default `false`.

Forcing a boolean through the string `Query` (returning `"true"`/`"false"`) was
rejected as an inelegant second indirection. Dynamic weights still reuse `Query`,
since a number in a string is natural where a boolean in a string is not.

### D5 — A condition is a spanned, reusable node

`Condition` is its own spanned `ScriptNode`, not a flag on the construct it
guards, so tooling can point at the exact condition and one node serves every
guard. Each construct references it through an optional property.

A separate guard node per construct was rejected: it splits one domain concept
into four, where reusing `Condition` keeps a single word and a single reader.

### D6 — Negation is deferred

"Jump unless X" is expressed today through a game-defined inverse flag
(`` `"NotRainy"?` ``). A prefix `!` was considered and rejected for now as
cryptic for non-technical writers; it can be added later without changing the
positive `?`.

### D7 — No in-script expression language

A condition is exactly one boolean query. There are no operators or comparisons
in the script; the game computes the meaning behind the key. Combining conditions
(`and`/`or`) is intentionally excluded — the game composes them behind a single
key.

## Markdown interaction

A condition is an inline code span, so Markdig parses it as inline code and an
ordinary Markdown preview shows `"FoundKey"?` as code — readable, and clearly not
spoken text. It collides with no existing Markdown or DialogueDown syntax: a
quoted string followed by `?` inside a code span is not a valid game call, so the
condition claims an otherwise-unused shape and removes no valid expressibility.

## Diagnostics

| Code      | Meaning                    | Kind   | Severity | When                                                                                        |
| --------- | -------------------------- | ------ | -------- | ------------------------------------------------------------------------------------------- |
| `DLG1106` | A condition guards nothing | Syntax | Error    | A `` `"key"?` `` condition is not a recognized guard on a jump, a line, or a choice option. |

`DLG1106` sits in the `DLG11xx` inline-surface band beside the other
inline-syntax diagnostics (`DLG1102` not-a-game-call, `DLG1104`/`DLG1105` choice
weights).

A malformed condition — a code span that is not a clean quoted query followed by
`?` — is not a condition at all; it falls back to game-call recognition and, if
that also fails, is reported as `DLG1102` and kept as literal text.

There is no invalid-value diagnostic: `Check` returns a boolean, so a condition
always resolves to true or false at runtime, and an unknown key defaults to
false.

## Deferred work

- **Runtime evaluation** — the compiler recognizes and preserves a condition, but
  reading the key through `Check` and acting on the result needs the runtime.
  Tracked with the [runtime work](https://github.com/pengzhengyi/dialoguedown/issues/45).
- **`IGameSystem.Check` as a public-API change** — adding `Check` breaks existing
  implementers, so whether to ship it as a required method (a clean break,
  acceptable before the runtime exists) or a default interface method
  (backward-compatible, falling back to parsing `Query`) is an implementation
  choice.
- **Negation** — a `not` form may follow once writer feedback shows the
  inverse-flag workaround is insufficient ([D6](#d6--negation-is-deferred)).
- **Expressions** — combining conditions is intentionally excluded for now
  ([D7](#d7--no-in-script-expression-language)).
