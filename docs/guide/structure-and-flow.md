# Structure and flow

How a script is organized into scenes and how a reader moves through it:
succession, choices, jumps, conditions, and the ways a run ends.

Part of the [script language specification](script-language.md).

## Table of contents

- [Succession](#succession)
- [Choices](#choices)
- [Random choices](#random-choices)
  - [Dynamic weights](#dynamic-weights)
- [Jumps](#jumps)
  - [Naming a jump](#naming-a-jump)
- [Conditional jumps](#conditional-jumps)
- [Conditional lines](#conditional-lines)
- [Conditional choices](#conditional-choices)
- [Conditional blocks](#conditional-blocks)
- [Ending a run](#ending-a-run)
- [Comments](#comments)
- [Front matter](#front-matter)
- [Authoring aids](#authoring-aids)

Dialogue sections become graph nodes and edges. Linear lines create succession
edges. Choices and jumps create branches.

## Succession

When one text line follows another, the second line is the only successor of the
first line. Separate successive speeches with a **blank line** so that each
speech is its own Markdown paragraph.

```markdown
Alice @A #main: Hello, Bob!

Bob @B #npc: Hello, Alice!
```

The script language follows standard Markdown line-break rules, so a Markdown
preview groups lines into speeches exactly as the compiler does:

- A **blank line** starts a new speech. This is the primary, most readable way to
  separate successive speeches.
- A **soft break** (a plain newline with no blank line) keeps both lines in the
  same speech. Use it to wrap one long speech across several source lines.
- A **hard break** starts a new speech without a blank line, for a compact
  layout. Make a break hard in either of the two standard Markdown ways: end the
  line with two or more trailing spaces, or end it with a backslash (`\`).

A soft break wraps a single speech across source lines; it is still one speech:

```markdown
Alice: This is a single long speech that the author wrapped across
several source lines for readability. It is still spoken as one speech.
```

A hard break separates two speeches without a blank line. The trailing backslash
below is one of the two hard-break forms; two trailing spaces are the other:

```markdown
Alice: Hello, Bob!\
Bob: Hello, Alice!
```

## Choices

Use `-` to offer selectable responses.

```markdown
Alice: The weather is nice today!
- Bob: Is it really?
- Bob: Yes, I agree.
```

Choices can be nested, but deep nesting becomes hard to scan. For a deeply
nested branch, consider moving it into a new scene and jumping to it instead.

```markdown
Alice: The weather is nice today!
- Bob: Is it really?
    - Alice: Yes. Let's play tennis!
- Bob: Yes, I agree.
    - Alice: Wonderful. Let's play tennis!
```

Choice **ordering** follows the list type. An **ordered** list (`1.`, `2.`, …)
means the choices must be presented in that textual order. An **unordered** list
(`-`) leaves later stages free to shuffle the display order — useful when the
options should appear in a random order.

## Random choices

Sometimes you want the *engine* to pick, not the player — a guard who greets you
one of several ways, a coin that lands heads or tails. Give an option a **weight**
— a code span ending in `%` — and the whole list becomes a **random choice**: at
runtime the engine selects exactly one option by weight and runs its body. No
menu is shown.

```markdown
The coin spins in the air.

- `50%` It lands heads.
- `50%` It lands tails.
```

Weights are **relative** — they are normalized by their sum, so equal numbers
mean equal odds no matter the total. Write them as the percentages you intend;
if they do not add up to 100, the compiler normalizes them and warns.

A bare `` `%` `` is an **auto** weight: it takes an equal share of whatever
percentage the explicit weights leave. Pin the odds that matter and let the rest
divide evenly.

```markdown
- `70%` Guard: Halt! Who goes there?
- `%`   Guard: ...oh, it's you.
```

Above, the auto weight resolves to the remaining 30%. Several autos split the
leftover equally, so `` `%` `` + `` `%` `` is a plain 50/50, and an all-auto list
is a convenient uniform random.

Each option's body is an ordinary choice body, so a random choice nests and
carries dialogue exactly like a player choice:

```markdown
- `80%` Merchant: Fresh apples!
    - Alice: I'll take one.
- `20%` Merchant: ...bad day for business.
```

A few rules keep random choices unambiguous:

- **Every option must carry a weight** once any option does. A bare option in a
  random choice is an error; write `` `%` `` to give it an equal share instead.
- **The weight comes first**, before the speaker. A code span ending in `%` is
  only a weight in this leading position; elsewhere it is ordinary inline code.
- **A Markdown preview** shows each weight as inline code (`50%`) at the start of
  the option — readable, and clearly not spoken text.

### Dynamic weights

A weight can also be **computed from game state**. Wrap a [key](game-state.md#quoting-a-key) in
a code span and end it with `%`; its runtime numeric result becomes the option's
weight. The key may be written unquoted here, as below:

```markdown
The rival spots you across the courtyard.

- `Bob.Affection%`       Bob: ...good to see you.
- `Christina.Affection%` Christina: Oh — hello.
```

The engine reads each query from your game, treats the result as a percentage,
and picks one line by weight — so the character most fond of the player is the
most likely to greet them. Static, auto, and dynamic weights mix freely in one
list:

```markdown
- `50%`              Guard: Halt!
- `Guard.Suspicion%` Guard: ...I'm watching you.
- `%`                Guard: Move along.
```

Because the numbers are unknown until the game runs, the compiler skips the
usual "weights should total 100%" check for any list that contains a dynamic
weight. At runtime:

- Each query must give a **finite number of 0 or more**. An answer that is
  missing, not a number, negative, or infinite is an error, and the engine picks
  no option.
- An auto weight takes an equal share of what the other weights in play leave of
  100, the numbers from the game included: `max(0, 100 − others) / autos`.
- The engine then normalizes the weights by their sum, as it does for static
  weights. It does not yet warn when the total is far from 100.

When every weight is 0, there is nothing to pick, so the random choice is passed
by and the run carries on with whatever follows the list, as it does when every
option is [left out](#conditional-choices):

```markdown
- `Bob.Affection%`       Bob: ...good to see you.
- `Christina.Affection%` Christina: Oh — hello.

You walk on across the courtyard.
```

While both affections are 0, the engine picks neither line, and the run goes
straight on to "You walk on across the courtyard."

To make sure the engine always has a line to pick, add an option with an auto
weight:

```markdown
- `Bob.Affection%`       Bob: ...good to see you.
- `Christina.Affection%` Christina: Oh — hello.
- `%`                    Nobody looks up.
```

While both affections are 0, the auto weight takes all of 100, so the line
"Nobody looks up." is always picked. Once the two affections add up to 100 or
more, the auto weight is 0, and that line has no chance of being picked.

## Jumps

A jump is `=>` followed by a Markdown-style link.

```ebnf
Jump = "=>" , MarkdownLink ;
```

Use same-file anchors for local dialogue and relative paths for cross-file
dialogue.

```markdown
=> [Play tennis](#play-tennis)

=> [Meet Bob](chapter-02.dialogue.md#meet-bob)
```

A jump into another file parses today, but the compiler does not resolve it yet:
it reports warning `DLG2016`, and reading continues past the jump as if it
were not there.

> [!NOTE]
> **A jump cannot appear inside a heading.** A heading marks a scene, which is
> itself a jump *target*, so jumping from within one is meaningless.
>
> ```markdown
> ## => [Play tennis](#play-tennis)
> ```
>
> The `=>` becomes plain heading text and the link stays an ordinary link — the
> heading reads as "`=>` Play tennis", not a jump.

<!-- Separate the two adjacent callouts (MD028). -->

> [!NOTE]
> **A jump must be written on a single line.** The `=>` and its link may be
> separated by spaces, but a line break between them ends the jump.
>
> ```markdown
> =>
> [Play tennis](#play-tennis)
> ```
>
> The `=>` becomes plain text and the link stays an ordinary link — two separate
> pieces, not a jump.

An arrow with no link after it is not a jump at all: it is read literally and
stays on the page as the two characters. Because an intended jump would
otherwise vanish without a trace, the compiler warns about it — including the
line-break case above, where the arrow and its link are split apart. To write the
characters deliberately, escape the arrow (`\=>`); the warning is for the arrow
you did not escape. See
[Literal punctuation](script-language.md#literal-punctuation).

```markdown
=> The market
```

Example:

```markdown
## Greetings

Alice: The weather is nice today!
- Bob: Is it really?
    - Alice: Yes. Let's play tennis!
        => [Play tennis](#play-tennis)
- Bob: Yes, I agree.
    - Alice: Wonderful. Let's play tennis!
        => [Play tennis](#play-tennis)

## Play tennis

Alice: Tennis is fun!

Bob: Yes, I agree.
```

### Naming a jump

A jump's link text is its **name**. The report draws it on the arrow the jump
becomes, and when a whole choice is a jump, it is the wording the player is
offered:

```markdown
- => [Take the east road](#the-market)
```

That option reads as "Take the east road". Writing the words twice — once as
speech and once in the link — is never necessary.

Sometimes the target needs no name of its own, because the words are already
there. Leave the link text empty:

```markdown
- Take the east road => [](#the-market)
```

The jump is real; it simply has nothing to be called, and the report draws it as
an unnamed jump.

> [!NOTE]
> **An autolink is not a jump.** `=> <#the-market>` is not part of the language:
> a jump is `=>` followed by a Markdown link, so the arrow finds nothing to jump
> to and stays on the page as two characters, with the usual warning. Write the
> link out, with an empty link text when the target needs no name of its own.

## Conditional jumps

Make a jump *optional* by placing a **condition** in front of it — a
[query](game-state.md#queries) with a `?` added inside the code span. The jump fires only when
the query reads as true:

```markdown
`FoundKey?` => [Open the vault](#the-vault)

The door stays shut. You look for another way.
```

If `FoundKey` is true, the reader jumps to *The Vault*; if it is false, the jump
is skipped and reading continues with the next line.

A condition is the third member of the query family: `` `"key"` `` inserts a
value, `` `key%` `` weights a random option, and `` `key?` `` reads a boolean.
The game decides what the key means and returns `true` or `false`; an unset value
counts as false.

The key may be written unquoted, as above — see [Quoting a key](game-state.md#quoting-a-key).
Quote it only when the key must *end* in a literal `?`; the trailing `?` after the
closing quote is then unmistakably the operator:

```markdown
`"Rainy?"?` => [Wait out the storm](#the-inn)
```

To branch when a flag is **false**, query a game-defined inverse — there is no
`not` operator:

```markdown
`NotRainy?` => [Set off across the moor](#the-moor)
```

A condition guards **one** jump. For an alternative, put another jump on the next
line, as its own paragraph:

```markdown
`FoundKey?` => [Open the vault](#the-vault)

=> [Search the study](#the-study)
```

If the key is found the reader takes the vault; otherwise the plain jump to the
study runs. Keep them in separate paragraphs: a jump is non-returning, so a
second jump in the same paragraph can never play — the compiler warns about the
unreachable content.

A conditional jump follows every [jump](#jumps) rule: it lives on one line, its
pieces may be separated by spaces but not a line break, and it cannot appear in a
heading.

## Conditional lines

Start a line with a **condition** to play the whole line only when the query
reads as true. The condition goes at the very start of the line, before the
speaker:

```markdown
`Angry?` Guard: You again? Get out.

The guard says nothing and waves you through.
```

If `Angry` is true, that line plays; if it is false, the line is skipped and
reading continues with the next line. The condition is not spoken — it sits
before the speaker, and `Guard` is still recognized as the speaker.

A line with no speaker can be conditional too:

```markdown
`Returned?` Welcome back. It has been too long.
```

As with a [conditional jump](#conditional-jumps), there is no `not` and no inline
*else*: query a game-defined inverse, or write the alternative as its own line —
often a second conditional line testing the opposite flag.

```markdown
`Angry?` Guard: You again? Get out.

`NotAngry?` Guard: Back so soon? Go on through.
```

A condition must lead the line it guards; a `` `key?` `` alone on a line, or one
buried mid-line after the speaker, has nothing to guard and is an error.

> [!NOTE]
> **One key cannot be both a line's condition and one of its queries.** A line
> asks your game for its condition and every query in what it says at once, and
> the game gives one answer per key: the condition needs true or false, the query
> words.
>
> ```markdown
> `Hero.Title?` Guard: Welcome, `"Hero.Title"`.
> ```
>
> The compiler rejects this as [`DLG2018`](error-codes.md#dlg2018). Give the
> condition a key of its own:
>
> ```markdown
> `Hero.HasTitle?` Guard: Welcome, `"Hero.Title"`.
> ```
>
> The same goes for a menu's option conditions and the queries in its options'
> words, and for a random choice's option conditions and
> [dynamic weights](#dynamic-weights). One key may still be a condition on one
> line and a query on another.

## Conditional choices

Front a **choice option** with a condition to offer it only when the query reads
as true. The condition goes at the very start of the option:

```markdown
- `HasKey?` Use the key on the lock.
- Search for another way in.
```

If `HasKey` is false, the first option is still offered but marked unavailable,
so the player can see it and cannot pick it. The condition guards the whole option, not just its
first line.

When every option is unavailable there is nothing to pick, so the menu is passed
by and the run carries on with whatever follows the list.

A [random option](#random-choices) can be conditional too — the condition comes
**first, before the weight**:

```markdown
- `IsAngry?` `50%` The guard glares and blocks your path.
- `30%` The guard waves you through.
- `20%` The guard ignores you.
```

When `IsAngry` is true the engine picks among all three by weight; when it is false
the first option is left out and the remaining weights are re-normalized — a random
pool with a dynamic set of options. A calm guard waves you through 60% of the time
and ignores you 40%.

An auto weight (`` `%` ``) counts only the options still in play. Give the last
option one:

```markdown
- `IsAngry?` `50%` The guard glares and blocks your path.
- `30%` The guard waves you through.
- `%` The guard ignores you.
```

An angry guard ignores you 20% of the time, the auto weight taking what `50%` and
`30%` leave of 100. A calm guard ignores you 70% of the time: with the first option
left out, the auto weight takes everything `30%` leaves.

When every option is left out there is nothing to pick, so the random choice is
passed by and the run carries on with whatever follows the list:

```markdown
- `IsAngry?` `%` The guard glares and blocks your path.
- `IsTired?` `%` The guard yawns and waves you through.

The guard says nothing.
```

A guard who is neither angry nor tired has no option left, so the run goes straight
on to "The guard says nothing."

As with a [conditional jump](#conditional-jumps) or line, each option's condition
is independent and there is no inline *else*: query an inverse flag for the
opposite case. A condition must lead the option it guards; one with nothing to
guard is an error.

## Conditional blocks

The [conditional line](#conditional-lines), [choice](#conditional-choices), and
[jump](#conditional-jumps) each guard **one** thing, on its own, with no *else*.
To guard a whole **group** — several lines, a jump, a command — and give it a
fallback, use a **conditional block**: `if` / `elseif` / `else` branches wrapped
in one blockquote.

```markdown
> `if` `Rich?`
>
> Guard: Ah — my lord. The gate is yours.
>
> => [The courtyard](#the-courtyard)
>
> `elseif` `Poor?`
>
> Guard: Back to the gutter with you.
>
> `else`
>
> Guard: I don't know your face. What business have you?
```

Zero or one branch plays: the first `if` or `elseif` whose condition reads as
true, or the optional `else` when none do. Without a matching condition or an
`else`, the whole block is skipped. A branch may hold as many lines as you like,
along with jumps and commands.

**One blockquote, kept connected.** Every branch — the `if`, each `elseif`, and
the `else` — lives in the **same** blockquote: each line starts with `>`, and the
blank line *between* branches is a bare `>` so the quote never breaks. The
blockquote itself marks where the block ends, so there is no closing keyword. If a
plain blank line (without `>`) splits the branches apart, the stray `elseif` or
`else` is an error — it is no longer connected to its `if`.

**Write the marker as two code spans** — the keyword and the condition —
`` `if` `` `` `Rich?` ``, not one. (`` `if Rich?` `` reads as a single condition on a
key named *if Rich*.) The `` `else` `` takes no condition.

**Nesting.** A branch can hold another conditional block, one level deeper with
`> >`:

```markdown
> `if` `Rich?`
>
> Guard: Welcome.
>
> > `if` `Armed?`
> >
> > Guard: But leave your blade at the post.
```

**Blank lines are quoted — and they close a nested branch.** The blank line
between utterances (which every line needs) is written with `>` inside a block, so
the quote never breaks. That quoted blank line is also what **ends a nested block**:
after a `> >` branch, a bare `>` line returns you to the outer branch. Omit it and
Markdown pulls the next line — even an `elseif` or `else` — *into* the nested
branch, so a marker that lands mid-paragraph is an error. Always write the bare `>`
line where a nested block ends.

**Choosing a scene by condition.** A conditional block groups *content*, not
scenes — a scene heading may not sit inside a branch. To send the reader to one of
several scenes by condition, use conditional [jumps](#conditional-jumps) to
top-level scenes instead:

```markdown
`Rich?` => [The lord's hall](#the-lords-hall)

`Poor?` => [The gutter](#the-gutter)
```

In a plain Markdown preview a conditional block renders as one indented, grouped
quote — a single bar down the side, a nested branch one level deeper.

As with every other conditional, there is no `not`: query a game-defined inverse
flag for the opposite case.

## Ending a run

`#END` is a reserved jump target that ends the current run. Jump to it to stop
the dialogue at a definite endpoint:

```markdown
Guide: The road ends here. Farewell, traveler.

=> [The end](#END)
```

`#END` is uppercase and reserved, so it can never collide with one of your
scenes: a heading's anchor is always lowercased (a `# End` heading anchors to
`#end`), while `#END` is matched exactly. There is no `# End` heading to write —
`#END` is always available as a target.

In a Markdown preview `=> [The end](#END)` renders as an ordinary link. Because
`#END` matches no heading, the link simply scrolls nowhere; the compiler
recognizes it as the reserved endpoint.

Without a jump, reading falls through from one scene to the next in the order
they are written, and the run ends after the last line of the file.

## Comments

Because the DSL is Markdown-inspired, use Markdown-compatible HTML comments for
author notes.

```markdown
Alice @A #main: Hello, Bob! <!-- Alice speaks in a warm tone. -->

Bob @B #npc: Hello, Alice!
```

## Front matter

A script may open with a **front matter** block — a `---`-fenced section of
metadata (title, tags, author, and the like) at the very top of the file. It is
never spoken and is **always discarded**, like a comment. Unlike authoring aids,
this is not configurable: metadata is never speech.

Only a block at the document start is front matter; a `---` later in the script
is a thematic break (an authoring aid).

```markdown
---
title: Reunion
tags: [chapter-1, intro]
---

Alice: Hello, Bob!
```

## Authoring aids

Markdown constructs that organize the script rather than say something —
**tables**, **fenced code blocks** (including diagrams like mermaid), and
**thematic breaks** (`---`) — are treated as author-only aids and are **ignored
by default**, much like comments. Use them freely to document
speakers, sketch scene relationships, or divide sections.

The web report renders a fenced `mermaid` block as a diagram while keeping it
outside the compiled dialogue:

````markdown
```mermaid
flowchart LR
    arrival[Arrival] --> choice{Trust the guide?}
    choice -->|yes| market[Market]
    choice -->|no| forest[Forest]
```
````

```markdown
<!-- A table of who appears in this scene — never spoken. -->

| Speaker | Mood  |
| ------- | ----- |
| Alice   | happy |
| Bob     | shy   |

Alice: Nice to see you, Bob!
```

> [!NOTE]
> Which unmodeled constructs are ignored or kept as dialogue text is
> configurable per project in `dialogue.toml`. See
> [Project configuration](configuration.md#unmodeled-markdown) for the defaults
> and override syntax.
