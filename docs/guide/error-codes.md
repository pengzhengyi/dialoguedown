# Error codes

DialogueDown reports each problem it finds as a **diagnostic** with a stable `DLG####` code, so a message is easy to look up. A code's leading digit names its category — `DLG1xxx` syntax, `DLG2xxx` semantic, `DLG3xxx` style — and each diagnostic has a default severity: <span class="dd-sev dd-sev--error">Error</span> (must be fixed), <span class="dd-sev dd-sev--warning">Warning</span> (compiles but is suspect), or <span class="dd-sev dd-sev--info">Info</span> (a neutral note). Placeholders such as `{0}` are filled with specifics — a name, a count — when the message is shown.

## Syntax (`DLG1xxx`)

The script's surface: text that does not parse as intended, or Markdown that never becomes dialogue.

### DLG1003

<span class="dd-sev dd-sev--warning">Warning</span> · Unreachable content after a jump

Content after a jump on this line can never play: a jump does not return, so anything following it is unreachable. Move it before the jump, or onto its own line.

A jump does not return, so text or a second jump after it on the same line never plays. Put each jump on its own line, separated by a blank line, so nothing trails it.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Crossroads
=&gt; [Market](#market) <mark class="dd-mark-bad">or =&gt; [Home](#home)</mark>

# Market
Merchant: Wares!

# Home
Alice: Cozy.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Crossroads
=&gt; [Market](#market)

<mark class="dd-mark-fix">=&gt; [Home](#home)</mark>

# Market
Merchant: Wares!

# Home
Alice: Cozy.</code></pre>

### DLG1101

<span class="dd-sev dd-sev--error">Error</span> · Tags without a speaker

"{0}" has tags but names no speaker for them to attach to. Begin the line with a name to declare a speaker (Alice #excited:), or with an @id to add tags to an already-declared one (@alice #excited:).

A line that begins with tags but no name has nothing to attach the tags to. Start the line with a speaker's name, or use an `@id` to add tags to a speaker already declared.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Scene
<mark class="dd-mark-bad">#excited</mark>: We made it!</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Scene
<mark class="dd-mark-fix">Alice </mark>#excited: We made it!</code></pre>

### DLG1102

<span class="dd-sev dd-sev--error">Error</span> · Not a game call

"{0}" is not a game call. Write a query that reads a value ("key"), a default command (("do something")), or a named command (Name("arg", ...)).

A code span calls into the game. Its contents must be a query that reads a value, a default command, or a named command — plain words are not a call.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Scene
Alice: The sky turns `<mark class="dd-mark-bad">just some words</mark>`.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Scene
Alice: The sky turns `<mark class="dd-mark-fix">&quot;World.Weather&quot;</mark>`.</code></pre>

### DLG1103

<span class="dd-sev dd-sev--error">Error</span> · Disallowed element in a label

{0} is not allowed inside a label or alt text; only text and styling are.

A jump or link label is plain, styled text only. Functional elements — code spans, images, nested links, or line breaks — are not allowed inside a label or an image's alt text.

### DLG1104

<span class="dd-sev dd-sev--error">Error</span> · Missing weight in a random choice

This option has no weight, but its list is a random choice. Give it a weight like `50%`, or `%` to share the remaining percentage equally.

In a random choice — a list where at least one option leads with a weight — every option must carry a weight so the engine can pick fairly. Give the option a percentage like `50%`, or `%` to share the remaining percentage equally.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- `50%` Heads.
<mark class="dd-mark-bad">- Tails.</mark></code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- `50%` Heads.
- <mark class="dd-mark-fix">`50%` Tails.</mark></code></pre>

### DLG1105

<span class="dd-sev dd-sev--error">Error</span> · Invalid choice weight

"{0}" is not a valid weight. Write a non-negative percentage like `50%`, or `%` to share the remaining percentage equally.

A choice weight is a percentage code span: a non-negative number like `50%`, a bare `%` to take an equal share of the remaining percentage, or a game-state key like `Luck%` the runtime computes into a weight. A negative number is not a valid weight.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- <mark class="dd-mark-bad">`-10%`</mark> Heads.
- `%` Tails.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- <mark class="dd-mark-fix">`10%`</mark> Heads.
- `%` Tails.</code></pre>

### DLG1106

<span class="dd-sev dd-sev--error">Error</span> · Condition guards nothing

A condition (`"{0}"?`) must guard a jump, line, choice option, or control branch. Put it immediately before a `=>` jump, at the start of a line or choice option, or after an `if`/`elseif` marker; otherwise remove the `?` to write a plain query.

A condition guards the jump it precedes, the line it fronts, the choice option it leads, or the control branch it opens. A `"key"?` code span anywhere else has nothing to guard. Move it to one of those positions, or remove the `?` to write a plain query.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Moor
Guide: <mark class="dd-mark-bad">`&quot;Rainy&quot;?`</mark> The moor is bleak.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Moor
<mark class="dd-mark-fix">`&quot;Rainy&quot;?`</mark> Guide: The moor is bleak.</code></pre>

### DLG1107

<span class="dd-sev dd-sev--warning">Warning</span> · Styled speaker prefix

This line looks like a speaker prefix ("{0}") but the name is styled, so it is not recognized and the line has no speaker. Remove the styling to declare the speaker.

A line that begins with a styled name followed by a colon — like `*Alice*:` — looks like a speaker prefix, but the styling stops it from being recognized, so the line has no speaker. Remove the styling from the name.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-bad">*Alice*:</mark> Hello there.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-fix">Alice:</mark> Hello there.</code></pre>

### DLG1108

<span class="dd-sev dd-sev--error">Error</span> · Severed control branch

`{0}` starts a separate blockquote without a connected `if`. Keep the `if`, every `elseif`, and the optional `else` inside one connected blockquote.

Every branch of a block conditional belongs to one connected blockquote. An `elseif` or `else` that starts another blockquote has no connected `if`; continue the original blockquote instead.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">&gt; <mark class="dd-mark-bad">`elseif`</mark> `Known?`
&gt;
&gt; Alice: Welcome back.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">&gt; <mark class="dd-mark-fix">`if`</mark> `Known?`
&gt;
&gt; Alice: Welcome back.</code></pre>

### DLG1109

<span class="dd-sev dd-sev--error">Error</span> · Malformed control branch order

`{0}` cannot appear here. A control block must contain one `if`, followed by zero or more `elseif` branches, then at most one `else`.

A block conditional has one `if`, then any `elseif` branches, then at most one `else`. Move a conditional branch before the fallback instead of adding another `else` afterward.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">&gt; `if` `Rich?`
&gt;
&gt; Alice: Welcome upstairs.
&gt;
&gt; `else`
&gt;
&gt; Alice: Try downstairs.
&gt;
&gt; <mark class="dd-mark-bad">`else`
&gt;
&gt; Alice: Welcome back.</mark></code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">&gt; `if` `Rich?`
&gt;
&gt; Alice: Welcome upstairs.
&gt;
&gt; <mark class="dd-mark-fix">`elseif` `Known?`
&gt;
&gt; Alice: Welcome back.</mark>
&gt;
&gt; `else`
&gt;
&gt; Alice: Try downstairs.</code></pre>

### DLG1110

<span class="dd-sev dd-sev--error">Error</span> · Control marker must stand alone

A `{0}` marker must stand alone in its paragraph. Put a quoted blank line (`>`) between the marker and its branch body.

A branch marker is its own paragraph. Keep the blockquote connected, but add a quoted blank line (`>`) before the branch body so Markdown does not fuse them together.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">&gt; `if` <mark class="dd-mark-bad">`Rich?`
&gt; Alice</mark>: Welcome upstairs.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">&gt; `if` <mark class="dd-mark-fix">`Rich?`
&gt;
&gt; Alice</mark>: Welcome upstairs.</code></pre>

### DLG1111

<span class="dd-sev dd-sev--error">Error</span> · Missing control branch condition

A `{0}` marker requires a condition in a separate code span, such as `{0}` `Rich?`.

An `if` or `elseif` marker needs its condition in a second code span. Add a condition such as `Rich?`; only `else` is unconditional.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-bad">&gt; `if`
&gt;</mark>
&gt; Alice: Welcome upstairs.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-fix">&gt; `if` `Rich?`</mark>
&gt;
&gt; Alice: Welcome upstairs.</code></pre>

### DLG1112

<span class="dd-sev dd-sev--error">Error</span> · Else branch cannot have a condition

An `else` marker cannot have the condition `{0}?`. Remove the condition for a fallback branch, or change `else` to `elseif`.

An `else` is the unconditional fallback, so it cannot carry a condition. Remove the condition, or write `elseif` when the branch should be conditional.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">&gt; `if` `Rich?`
&gt;
&gt; Alice: Welcome upstairs.
&gt;
&gt; <mark class="dd-mark-bad">`else` `Known?`</mark>
&gt;
&gt; Alice: Welcome back.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">&gt; `if` `Rich?`
&gt;
&gt; Alice: Welcome upstairs.
&gt;
&gt; <mark class="dd-mark-fix">`else`</mark>
&gt;
&gt; Alice: Welcome back.</code></pre>

### DLG1113

<span class="dd-sev dd-sev--warning">Warning</span> · Dangling jump arrow

`=>` makes a jump only when a link follows it. With no link here it is read literally, staying as the characters "=>". If you meant to jump, add a target: `=> [The market](#the-market)`.

`=>` is the jump sigil: it becomes a jump only when a Markdown link follows it. With no link there is nothing to jump to, so the arrow is read literally — it stays on the page as the two characters and the script simply continues to the next line. That is fine when you meant to type an arrow; when you meant to jump, give it a target.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Crossroads
Alice: Which way?

<mark class="dd-mark-bad">=&gt; The market</mark>

# The market
Merchant: Wares!</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Crossroads
Alice: Which way?

<mark class="dd-mark-fix">=&gt; [The market](#the-market)</mark>

# The market
Merchant: Wares!</code></pre>

### DLG1114

<span class="dd-sev dd-sev--info">Info</span> · Markdown left out of the script

This {0} is not dialogue, so the compiler left it out of the script. That is expected for notes and diagrams; write it as dialogue if it should be spoken.

DialogueDown models the Markdown a dialogue needs; everything else is an authoring aid. A code block, a table, or a divider is left out of the script rather than spoken, which is usually the point — a diagram or a note belongs beside the dialogue, not in it. If that is what you meant, keep it: this is a note, not a fault, and nothing about the compile changes. It exists so the omission is never a surprise. If the construct was meant to shape the dialogue, write it in DialogueDown's own terms — a scene break is a heading. If it arrived by accident, remove it.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Chapter One

Alice: We should go.

<mark class="dd-mark-bad">---</mark>

Alice: The road was long.</code></pre>

<span class="dd-eg-fix">Fix — if it was meant to break the scene</span>

<pre class="dd-example"><code class="nohighlight"># Chapter One

Alice: We should go.

<mark class="dd-mark-fix"># On The Road</mark>

Alice: The road was long.</code></pre>

<span class="dd-eg-fix">Fix — if it arrived by accident</span>

<pre class="dd-example"><code class="nohighlight"># Chapter One

Alice: We should go.

<mark class="dd-mark-fix">Alice: The road was long.</mark></code></pre>

## Semantic (`DLG2xxx`)

A meaning-level problem found during analysis — a reference that does not resolve, or a conflict.

### DLG2001

<span class="dd-sev dd-sev--error">Error</span> · Duplicate scene anchor

Two scenes resolve to the same anchor '#{0}'. Rename one heading so each jump target is unambiguous.

Each scene heading becomes a jump target — an anchor slugged from its text. Two headings with the same text produce the same anchor, so a jump to it is ambiguous.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Chapter
Alice: Hello.

# Chapter
Bob: Goodbye.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Chapter<mark class="dd-mark-fix"> One</mark>
Alice: Hello.

# Chapter<mark class="dd-mark-fix"> Two</mark>
Bob: Goodbye.</code></pre>

### DLG2002

<span class="dd-sev dd-sev--error">Error</span> · Heading without an anchor

A heading needs at least one letter or number so it can be a jump target; this one has none. Add sluggable text to the heading.

A heading becomes a jump target only if it has letters or numbers to slug into an anchor. A heading of punctuation alone can never be jumped to.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># <mark class="dd-mark-bad">...</mark>
Alice: Hello.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># <mark class="dd-mark-fix">Prologue</mark>
Alice: Hello.</code></pre>

### DLG2003

<span class="dd-sev dd-sev--error">Error</span> · Ambiguous speaker binding

Cannot bind name '{0}' to id '@{1}': both are already in use as separate speakers, so joining them now is ambiguous. If they are the same speaker, declare it (Name @{1}: …) before either is used on its own.

A name and an `@id` were each used on their own for different speakers, so binding them together now is ambiguous. Declare the pairing once, up front, before either is used alone.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">Alice: Hello.

@A: Over here.

<mark class="dd-mark-bad">Alice @A</mark>: It is me.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-fix">Alice @A</mark>: It is me.

Alice: Hello.

@A: Over here.</code></pre>

### DLG2004

<span class="dd-sev dd-sev--error">Error</span> · Id bound to two names

id '@{0}' is already bound to speaker '{1}', so it cannot also be bound to '{2}'. Use a different id for '{2}'.

An `@id` is a stable handle for one speaker, so it cannot name two. Give the second speaker its own id.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">Alice @A: Hi.

<mark class="dd-mark-bad">Bob @A</mark>: Hello.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">Alice @A: Hi.

Bob <mark class="dd-mark-fix">@B</mark>: Hello.</code></pre>

### DLG2005

<span class="dd-sev dd-sev--error">Error</span> · Name bound to two ids

Speaker '{0}' is already bound to id '@{1}', so it cannot also be bound to id '@{2}'. Give the speaker a single id.

A speaker has one stable `@id`. Binding the same name to a second id is a conflict — give the speaker a single id everywhere.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">Alice @A: Hi.

<mark class="dd-mark-bad">Alice @B</mark>: Hello again.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">Alice @A: Hi.

Alice @A: Hello again.</code></pre>

### DLG2006

<span class="dd-sev dd-sev--error">Error</span> · More than one default speaker

Two speakers are marked ##default ('{0}' and '{1}'); only one default speaker is allowed.

The default speaker covers lines that name no one, so a script can have only one. Mark just a single speaker `##default`.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-bad">Alice ##default</mark>: Hi.

<mark class="dd-mark-bad">Bob ##default</mark>: Hello.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-fix">Alice ##default</mark>: Hi.

Bob: Hello.</code></pre>

### DLG2007

<span class="dd-sev dd-sev--error">Error</span> · Unnamed speaker id

Speaker '@{0}' is used but never declared with a name. Declare it with a name (Name @{0}: …) — a stable id must belong to a named speaker.

A stable `@id` must belong to a named speaker. This id is referenced but never declared with a name — declare it once with `Name @id:`.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Scene
<mark class="dd-mark-bad">@ghost</mark>: Who goes there?</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Scene
<mark class="dd-mark-fix">Ghost </mark>@ghost: Who goes there?</code></pre>

### DLG2008

<span class="dd-sev dd-sev--error">Error</span> · Unknown reserved tag

'##{0}' is not a known reserved tag. Use a custom tag ('#{0}') or one of DialogueDown's reserved tags.

A `##name` tag is a reserved, built-in tag, and `##default` is the only one DialogueDown knows. For your own metadata use a custom tag with a single `#`.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Scene
Alice <mark class="dd-mark-bad">##hero</mark>: To the rescue!</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Scene
Alice <mark class="dd-mark-fix">#hero</mark>: To the rescue!</code></pre>

### DLG2009

<span class="dd-sev dd-sev--error">Error</span> · Jump to a missing scene

Jump target '#{0}' does not match any scene. Check the anchor, or add a heading it can point to.

A jump must point at a scene that exists in the file. This jump's anchor matches no heading — check the spelling, or add the scene it should reach.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Start
Alice: Onward!

=&gt; [Continue](<mark class="dd-mark-bad">#the-end</mark>)</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Start
Alice: Onward!

=&gt; [Continue](#the-end)

<mark class="dd-mark-fix"># The End</mark>
Alice: We made it.</code></pre>

### DLG2010

<span class="dd-sev dd-sev--error">Error</span> · Random choice weights sum to zero

Every weight in this random choice is 0, so no option can be selected. Give at least one option a positive weight.

A random choice picks one option by weight. When every weight is 0 there is nothing to pick from — the odds are undefined. Give at least one option a positive weight.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- <mark class="dd-mark-bad">`0%`</mark> Heads.
- `0%` Tails.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- <mark class="dd-mark-fix">`50%`</mark> Heads.
- `50%` Tails.</code></pre>

### DLG2015

<span class="dd-sev dd-sev--error">Error</span> · Scene heading inside a branch

A scene heading must be a document-level block; it cannot appear inside a control branch or choice option. Move the heading outside the branch, then jump to that scene when the branch should enter it.

Scene headings define document-level jump targets. A heading inside a control branch or choice option would not create a scene, so move it outside the branch and jump to it when that path should enter the scene.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">&gt; `if` `Rich?`
&gt;
<mark class="dd-mark-bad">&gt; # Upstairs</mark>
&gt;
&gt; Alice: Welcome.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"><mark class="dd-mark-fix"># Upstairs</mark>

&gt; `if` `Rich?`
&gt;
&gt; Alice: Welcome.</code></pre>

### DLG2016

<span class="dd-sev dd-sev--warning">Warning</span> · Jump outside this script is not resolved yet

This jump names '{0}', which is outside this script. Targets outside the script are not resolved yet, so the jump leads nowhere. Point it at a scene in this script — '#the-scene' — until cross-file jumps land.

A jump reaches a scene in the script it is written in. Reaching one in another script is not built yet, so a target naming a file or a URL resolves to nothing and the line simply reads on. Keep the destination in this script until cross-file jumps land.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">Alice: To the vault.

=&gt; [The vault](<mark class="dd-mark-bad">chapter-02.md#the-vault</mark>)</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">Alice: To the vault.

=&gt; [The vault](<mark class="dd-mark-fix">#the-vault</mark>)

# The vault</code></pre>

### DLG2017

<span class="dd-sev dd-sev--warning">Warning</span> · Option with nothing to show

Nothing here names this option, so a player is offered a blank line to pick. Give the option something to say, or name the jump it makes: `- => [Take the east road](#the-market)`.

A menu shows each option by the words written in it — the line it speaks, or the text of the jump it makes. An option with neither leaves the player a blank line to pick. The compiler will not read words off the scene the option leads to, because those belong to whoever wrote them, so the option stays as blank as it was written.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight">Alice: Which way?

<mark class="dd-mark-bad">- `(&quot;fade out&quot;)`</mark>
- Alice: Stay here.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight">Alice: Which way?

<mark class="dd-mark-fix">- Slip away quietly `(&quot;fade out&quot;)`</mark>
- Alice: Stay here.</code></pre>

## Style (`DLG3xxx`)

A valid script that reads correctly but could read better.

### DLG3002

<span class="dd-sev dd-sev--warning">Warning</span> · Deeply nested choice branch

This branch reaches choice nesting level {0}; the recommended maximum is {1}. Consider moving this branch into a new scene and jumping to it instead.

Nested choices remain valid, but a fourth level becomes difficult to scan and maintain. Consider moving that branch into a new scene and jumping to it instead.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Conversation

- Level 1
    - Level 2
        - Level 3
            <mark class="dd-mark-bad">- Level 4</mark>
                Alice: This branch is difficult to scan.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Conversation

- Level 1
    - Level 2
        <mark class="dd-mark-fix">=&gt; [Continue](#deeper-branch)</mark>

<mark class="dd-mark-fix"># Deeper branch</mark>

- Level 3
    - Level 4
        Alice: This branch is easier to scan.</code></pre>

### DLG3003

<span class="dd-sev dd-sev--warning">Warning</span> · Choice weights do not total 100%

These weights total {0}%, not 100%. Weights are normalized by their sum, so the odds still work; adjust them to total 100% to state the intended odds directly.

A random choice's weights are relative — they are normalized by their sum — so any positive total works. When they do not add up to 100 the intended odds are harder to read; adjust them to total 100% (or use `%` to share the rest) to state the odds directly.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- `50%` Heads.
- <mark class="dd-mark-bad">`30%`</mark> Tails.</code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- <mark class="dd-mark-fix">`50%`</mark> Heads.
- `50%` Tails.</code></pre>

### DLG3004

<span class="dd-sev dd-sev--warning">Warning</span> · Single-option random choice

This random choice has a single option, so it is always selected and the weight has no effect. Remove the weight to make it a plain line, or add more options.

A random choice with only one option always selects it — the weight has no effect and the list is not really random. This usually means a plain line was given a weight, or the other options are missing.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- <mark class="dd-mark-bad">`50%` It always lands heads.</mark></code></pre>

<span class="dd-eg-fix">Fix</span>

<pre class="dd-example"><code class="nohighlight"># Coin
The coin spins.

- `50%` Heads.
- <mark class="dd-mark-fix">`50%` Tails.</mark></code></pre>
