# Error codes

DialogueDown reports each problem it finds as a **diagnostic** with a stable `DLG####` code, so a message is easy to look up. A code's leading digit names its category — `DLG1xxx` syntax, `DLG2xxx` semantic, `DLG3xxx` style — and each diagnostic has a default severity: <span class="dd-sev dd-sev--error">Error</span> (must be fixed), <span class="dd-sev dd-sev--warning">Warning</span> (compiles but is suspect), or <span class="dd-sev dd-sev--info">Info</span> (a neutral note). Placeholders such as `{0}` are filled with specifics — a name, a count — when the message is shown.

## Syntax (`DLG1xxx`)

A line's surface does not parse as intended.

### DLG1003

<span class="dd-sev dd-sev--warning">Warning</span> · Multiple jumps on a line

This line has {0} jumps; multiple jumps on one line run in sequence and are easy to misread — prefer at most one.

Two jumps on one line run one after the other, which is easy to misread. Put each jump on its own line, separated by a blank line, so the flow is clear.

<span class="dd-eg-bad">Triggering example</span>

<pre class="dd-example"><code class="nohighlight"># Crossroads
<mark class="dd-mark-bad">=&gt; [Market](#market) or =&gt; [Home](#home)</mark>

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

A choice weight is a percentage code span. Write a non-negative number like `50%`, or a bare `%` to take an equal share of the remaining percentage. A negative number or other text is not a valid weight.

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
