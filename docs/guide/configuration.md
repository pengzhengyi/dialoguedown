# Project configuration

A `dialogue.toml` file configures how DialogueDown compiles the scripts in your
project — the **speakers** your dialogue uses (and which one is the **default**)
and the **compilation mode**. It sits at your project root, and the
`ddown` CLI finds it automatically, so your scripts stay free of
project-wide setup.

> [!NOTE]
> DialogueDown is in early development. Configuration currently covers speakers
> and the compilation mode; more knobs (documented in the design notes) will
> follow.

## Table of contents

- [Project configuration](#project-configuration)
  - [Table of contents](#table-of-contents)
  - [Where the file lives](#where-the-file-lives)
  - [Configuring speakers](#configuring-speakers)
  - [The default speaker](#the-default-speaker)
  - [Tags](#tags)
  - [Compilation mode](#compilation-mode)
  - [Using it with the CLI](#using-it-with-the-cli)
  - [Autocompletion in the report](#autocompletion-in-the-report)

## Where the file lives

Put a `dialogue.toml` at your **project root** — the folder your scripts live
under:

```text
my-game/
  dialogue.toml
  dialogues/
    act1/
      intro.dialogue.md
```

The CLI **discovers** it by walking up from a script's folder to the nearest
`dialogue.toml`, so one file at the root serves every script beneath it — the same
way `tsconfig.json`, `.clang-format`, and `pyproject.toml` are found. To point at a
config elsewhere, pass [`--config`](#using-it-with-the-cli).

## Configuring speakers

Declare each speaker as a `[[speakers]]` entry:

```toml
# dialogue.toml

[[speakers]]
name    = "Narrator"     # required
id      = "narrator"     # optional stable id, referenced in a script as @narrator
default = true           # this speaker voices lines with no speaker (at most one)

[[speakers]]
name = "Alice"
id   = "A"
tags = ["main", "mood=cheerful"]
```

| Key       | Required | Meaning                                                                 |
| --------- | -------- | ----------------------------------------------------------------------- |
| `name`    | yes      | The speaker's display name; must be non-empty.                          |
| `id`      | no       | A stable `@id` a script can reference instead of the name.              |
| `default` | no       | `true` marks the document's default speaker; at most one may set it.    |
| `tags`    | no       | Content tags for the speaker (see [Tags](#tags)).                       |

A configured speaker behaves exactly like one
[declared in a script](script-language.md#speaker): the two unify when they share a
name, so `Alice: Hi.` in a script and the `Alice` entry above are one speaker.

## The default speaker

When a line has no speaker prefix, it belongs to the **default speaker**. A script
can name its own default with the [`##default`](script-language.md#default-speaker)
tag; when it does not, the speaker you mark `default = true` in `dialogue.toml` fills
those lines instead of the built-in anonymous fallback.

Precedence, highest first:

1. A script's own in-file `##default`.
2. The configured `default = true` speaker.
3. The anonymous fallback (no configuration, no in-file default).

## Tags

The `tags` array accepts the same tags a script uses. Each entry is either a plain
name or a `name=value` pair:

```toml
tags = ["main", "mood=cheerful"]
```

For a tag whose name itself contains `=`, use an inline table:

```toml
tags = [{ name = "quest=intro", value = "started" }]
```

## Compilation mode

A top-level `mode` key chooses **how far a compile proceeds after an error**:

```toml
# dialogue.toml
mode = "best-effort"
```

| Value | Behavior |
| --- | --- |
| `stage-boundary` | Recover within a stage and report every error it finds, then stop at the stage boundary. The default when the key is unset. |
| `best-effort` | Recover through every stage and collect everything, for the fullest picture. |

The CLI's [`--mode`](#using-it-with-the-cli) option overrides the configured mode
for a single run, so the order of precedence is **`--mode` > `dialogue.toml` >
the default**. For the rationale — and why the `fail-fast` mode is an embedding
contract rather than a settable value — see the
[Compilation Mode Configuration](../contributing/design-notes/Compilation%20Mode%20Configuration.md)
design note.

## Using it with the CLI

Both commands pick up the configuration automatically:

```bash
# Discovers the nearest dialogue.toml walking up from the script's folder.
ddown compile dialogues/act1/intro.dialogue.md
ddown visualize dialogues/act1/intro.dialogue.md
```

Use `--config` to name a specific file, overriding discovery:

```bash
ddown compile intro.dialogue.md --config config/dialogue.toml
```

| Option            | Behavior                                                              |
| ----------------- | --------------------------------------------------------------------- |
| *(none)*          | Discover the nearest `dialogue.toml` from the script's folder upward. |
| `--config <path>` | Use exactly this file; a missing path is a usage error.               |
| `--mode <mode>`   | Override the configured mode for this run.                            |

> [!NOTE]
> For `visualize`, discovery stays within `--root` (the served folder), so a report
> never reads a config outside what you chose to serve. A malformed `dialogue.toml`
> is reported with its file, line, and column.

## Autocompletion in the report

Because configured speakers are part of the compiled model, the interactive
[`visualize`](overview.md) report knows them: the source editor **autocompletes your
configured speakers** — including ones you have declared in `dialogue.toml` but not
yet used in the script — alongside the speakers, `@id`s, and tags the script itself
introduces.
