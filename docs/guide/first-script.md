# Your first script

Write a short branching conversation and watch the compiler take it apart. By the
end you will have a working script and will have met every idea the
[specification](script-language.md) covers in depth.

You need the `ddown` CLI — see [Command line](cli.md) if you have not installed it
yet.

## Table of contents

- [1. Say one line](#1-say-one-line)
- [2. Give the line a speaker](#2-give-the-line-a-speaker)
- [3. Split the script into scenes](#3-split-the-script-into-scenes)
- [4. Offer the player a choice](#4-offer-the-player-a-choice)
- [5. Jump somewhere](#5-jump-somewhere)
- [6. Talk to your game](#6-talk-to-your-game)
- [7. End the run](#7-end-the-run)
- [Where to go next](#where-to-go-next)

## 1. Say one line

Create `lighthouse.dialogue.md`:

```markdown
The lamp turns, and the sea answers.
```

Compile it:

```bash
ddown compile lighthouse.dialogue.md
```

That is a valid script. A plain paragraph is a **line** of dialogue — no
ceremony required, because a script is Markdown first.

## 2. Give the line a speaker

Put a name before a colon:

```markdown
Ada: The lamp turns, and the sea answers.
```

`Ada` is now the **speaker**. Declare her once with an `@id` and the compiler
will recognize her by either name from then on:

```markdown
@keeper Ada

Ada: The lamp turns, and the sea answers.
keeper: Forty years, and it has never missed a night.
```

Lines with no speaker at all fall to a **default speaker**, so narration needs no
prefix. See [Speakers and lines](speakers-and-lines.md) for styling, tags, and
images.

## 3. Split the script into scenes

A Markdown heading starts a **scene**, and its GitHub-style anchor is how other
parts of the script refer to it:

```markdown
# Arrival

Ada: You made it through the squall.

# Departure

Ada: Safe travels, sailor.
```

`# Arrival` has the anchor `#arrival`. Without any branching, a reader simply
falls through the scenes in the order they are written.

## 4. Offer the player a choice

A Markdown list becomes a **choice**:

```markdown
# Arrival

Ada: You made it through the squall.

- [Ask about the light](#the-light)
- [Ask about the storm](#the-storm)
```

Each option is a link to the scene it leads to. To let the *engine* pick instead
of the player, give the options weights with `` `%` ``:

```markdown
- `60%` [She smiles](#warm)
- `40%` [She says nothing](#cold)
```

## 5. Jump somewhere

`=>` sends the reader to another scene:

```markdown
# The Light

Ada: It has burned every night for forty years.

=> [Departure](#departure)
```

A jump on a line of its own is a **control line** — it carries an effect, not
speech, so it is never attributed to a speaker.

## 6. Talk to your game

An inline code span is how a script reaches your game. A **query** asks a
question, a **command** tells the game to act:

```markdown
Ada: `"met_ada"?` Good to see you again, sailor.

Ada: Take this. `give("lantern")`
```

The query `` `"met_ada"?` `` fronts the line, so the line plays **only** when
your game answers true. The same condition can guard a choice or a jump. See
[Game state](game-state.md).

## 7. End the run

`#END` is the reserved target that stops the script:

```markdown
Ada: Safe travels, sailor.

=> [The end](#END)
```

## Where to go next

Compile the finished script and open the interactive report to see each compiler
stage:

```bash
ddown visualize lighthouse.dialogue.md
```

| Next | Why |
| --- | --- |
| [Structure and flow](structure-and-flow.md) | Conditional blocks, nested choices, and the rest of the flow constructs. |
| [Speakers and lines](speakers-and-lines.md) | Styling, tags, and images on a line. |
| [Project configuration](configuration.md) | Declare speakers project-wide in `dialogue.toml`. |
| [Error codes](error-codes.md) | What the compiler tells you when something is wrong. |
