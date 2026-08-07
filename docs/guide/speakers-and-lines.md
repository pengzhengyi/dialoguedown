# Speakers and lines

Every line of speech in a DialogueDown script, and everything you can put on
one: who is speaking, how the text is styled, and the tags and images a line can
carry.

Part of the [script language specification](script-language.md).

## Table of contents

- [Speaker](#speaker)
  - [Inline speaker declaration](#inline-speaker-declaration)
  - [Speaker reference](#speaker-reference)
  - [Partial declaration](#partial-declaration)
  - [Default speaker](#default-speaker)
- [Whitespace around the colon](#whitespace-around-the-colon)
- [Styling](#styling)
- [Tags](#tags)
- [Images](#images)

A text line is the basic unit of spoken dialogue.

```ebnf
TextLine = [ Speaker , ":" ] , Speech ;
```

Canonical form:

```markdown
Alice: Hello, Bob!
```

## Speaker

A line may name who speaks before the colon. A speaker prefix is either a
**declaration** (which binds a name, an optional id, and tags) or a **reference**
(which only points at a known speaker); omitting the prefix uses the default
speaker.

> [!NOTE]
> A speaker's name must be **plain text**. If you style it — `*Alice*:`,
> `**Alice**:` — it is not read as a speaker prefix, so the line is left
> unattributed. Write the name unstyled (`Alice:`).

### Inline speaker declaration

Inline speaker declarations are a lightweight way to introduce or enrich
speakers directly in script.

```ebnf
SpeakerName          = Identifier | String ;
SpeakerId            = Identifier ;
SpeakerDeclaration   = SpeakerName , [ "@" , SpeakerId ] , Tags ;
```

Example:

```markdown
Alice @A #main: Hello, Bob!
Bob @B #npc: Hello, Alice!
Alice #avatar="alice.png": The weather is nice today!
```

Inline declarations may appear multiple times as long as they don't conflict with
existing speaker identity. Conflicting speaker metadata is a compile-time error.

A prefix counts as a *declaration* only when it binds metadata **with a name** — a
name plus an `@id` and/or tags. A prefix that is **only** a name (`Alice:`) or
**only** an `@id` (`@A:`) is a [speaker reference](#speaker-reference); an `@id`
**with** tags but no name is a [partial declaration](#partial-declaration).

> [!NOTE]
> Speaker tags apply globally to the speaker, not just to the single text line
> where the tag appears.

### Speaker reference

Speakers can be referenced by name or by stable ID.

```ebnf
SpeakerReference = SpeakerName | "@" , SpeakerId ;
```

Examples:

```markdown
Alice: Hello, Bob!

@A: Hello, Bob!
```

Stable IDs are useful when a character has nicknames, localized names, or
multiple display names.

For long-form stories, keep a central `speakers.json` file so speaker identity
and metadata have a single source of truth.

```json
[
  {
    "name": "Alice",
    "id": "A",
    "tags": ["main"]
  },
  {
    "name": "Bob",
    "id": "B",
    "tags": ["npc"]
  }
]
```

### Partial declaration

An `@id` **with** tags but no name is a *partial declaration*: it references a
speaker by id and contributes extra tags to them.

```ebnf
PartialSpeakerDeclaration = "@" , SpeakerId , Tags ;
```

```markdown
@A #excited: I can't wait to see the festival!
```

The referenced speaker is resolved and the tags are merged into their metadata (a
no-conflict merge) during compilation. Tags with **neither** a name nor an `@id`
(`#tag:`) have no speaker to attach to and are a compile-time error.

### Default speaker

When a line omits `Speaker`, the compiler will use the default speaker. If no
default speaker exists, it will use the system speaker.

```markdown
The narrator speaks because no explicit speaker is provided.
```

Mark a speaker as the default speaker with the reserved `##default` tag:

```markdown
Narrator @narrator ##default: The story begins.

This line is also spoken by Narrator.
```

## Whitespace around the colon

Whitespace around the colon is flexible for author comfort. Whitespace before
the colon and *all* whitespace immediately after it is insignificant, so every
line below means the same thing — Alice saying `Hello, Bob!`:

```markdown
Alice:Hello, Bob!

Alice :Hello, Bob!

Alice: Hello, Bob!

Alice :   Hello, Bob!
```

The amount of spacing never changes the speech. This matters because rendered
Markdown collapses consecutive spaces visually, so relying on extra spaces would
be an invisible trap — the author could not see the difference in a preview.

If speech must start with a literal leading space, quote it. Quoting is the
single, explicit way to preserve leading whitespace:

```markdown
Alice: " Hello, Bob!"
```

## Styling

Speech may use standard Markdown emphasis and strikethrough for **styling**:

```markdown
Alice: I *really* mean it.

Alice: This is **very** important.

Alice: That plan is ~~canceled~~.
```

- `*text*` or `_text_` is **italic**; `**text**` or `__text__` is **bold**;
  `~~text~~` is **strikethrough**. Combine emphasis (`***text***`) for bold italic.
- To type a **literal** asterisk, underscore, or tilde, escape it (`\*`, `\_`,
  `\~`). Underscores inside a word (`snake_case_name`) are never emphasis, and a
  single `~` is not strikethrough — only `~~...~~` is.

Styling can wrap other speech constructs — a query inside bold still resolves:

```markdown
Bob: **Hello `"MainCharacter.Name"`!**
```

The result is a bold *Hello Alice!*.

> [!NOTE]
> This spec defines *what* styling an author can write. How a given style renders
> (color, bold weight, BBCode, plain text, …) is decided by the game's
> presentation layer, not by the compiler.

## Tags

Tags attach metadata that plugins, tools, or runtime systems can interpret.

```ebnf
TagName          = Identifier | String ;
TagGroupName     = Identifier | String ;
Tag              = "#" , TagName ;
TagGroup         = "#" , TagGroupName , "=" , TagName ;
ReservedTag      = "##" , TagName ;
ReservedTagGroup = "##" , TagGroupName , "=" , TagName ;
Tags             = { Tag | TagGroup | ReservedTag | ReservedTagGroup } ;
```

Examples:

```markdown
Alice @A #main: Hello, Bob!

Alice #mood=happy: What a beautiful day.

Alice #"speaker tone"="warm": I'm glad to see you.

Narrator @narrator ##default: The story begins.
```

**Custom tags** (`#...`) are project-defined, opaque metadata. **Reserved tags**
(`##...`) are built-in language tags owned by DialogueDown, drawn from a known
set. A tag that carries a value (`#name=value`) is a **tag group**.

Tags may appear wherever they attach to content: in a **speaker declaration**, in
a **link or image label**, and **anywhere within speech text**. A custom or
reserved tag must never start a line at block scope — a tag always rides along
with the element it annotates, never standing alone as a line.

Currently, the only supported reserved tag is `##default`, which marks a speaker
as the default speaker.

## Images

Embed an image inline in speech with standard Markdown image syntax; it appears
at that position in the line:

```markdown
Alice: Here is the photo you wanted. ![sunset](sunset.png)
```

Presentation **tags** may be added inside the **alt** text to customize how the
image is shown (size, alignment, or any hint the game defines), using the same
`#tag` / `#group=value` form as speaker tags:

```markdown
Alice: ![Alice smiling #size=small #align=left](alice.png)
```

The compiler keeps the source path and the alt text (including any tags) exactly
as written; the presentation layer decides what the tags mean and how the image
renders.
