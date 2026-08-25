# Implementation note: Configuration loader

> [!IMPORTANT]
> Status: **implemented**. The **edge** that reads a
> project's `dialogue.toml` and produces a [`CompilerOptions`](./Configuration.md)
> for the compiler. It lives in its own satellite assembly so the engine-agnostic
> core stays free of a TOML dependency: the core keeps taking a plain
> `CompilerOptions` value, and this loader is the optional, file-backed way to
> build one.

## Table of contents

- [Implementation note: Configuration loader](#implementation-note-configuration-loader)
  - [Table of contents](#table-of-contents)
  - [Goal and scope](#goal-and-scope)
  - [Where it sits](#where-it-sits)
  - [Ubiquitous language](#ubiquitous-language)
  - [The `dialogue.toml` schema](#the-dialoguetoml-schema)
  - [Functionality checklist](#functionality-checklist)
  - [Interfaces and abstractions](#interfaces-and-abstractions)
  - [Key design decisions](#key-design-decisions)
    - [DD1 — A satellite assembly with Tomlyn, not a core dependency](#dd1--a-satellite-assembly-with-tomlyn-not-a-core-dependency)
    - [DD2 — Traverse Tomlyn's syntax tree, not a fixed POCO](#dd2--traverse-tomlyns-syntax-tree-not-a-fixed-poco)
    - [DD3 — Reserved as typed keys, custom as shorthand with a structured escape hatch](#dd3--reserved-as-typed-keys-custom-as-shorthand-with-a-structured-escape-hatch)
    - [DD4 — Validate at the edge, fail with a location](#dd4--validate-at-the-edge-fail-with-a-location)
  - [Error and boundary cases](#error-and-boundary-cases)
  - [Integration](#integration)
  - [Testability](#testability)
  - [Deferred](#deferred)

## Goal and scope

The [Configuration](./Configuration.md) component keeps the core
**binding-agnostic**: the compiler takes a `CompilerOptions` object and never
reads a file. This component is the **edge** that turns a project's
`dialogue.toml` into that object — parsing the document, delegating each section
to a focused schema reader, and **validating** before anything reaches the
compiler.

**In scope:** a `TomlConfigurationLoader` that reads a TOML file (or string) into
a `CompilerOptions`; the `mode`, `[[speakers]]`, and
`[markdown.unmodeled]` schemas; edge validation; and a
`DialogueConfigurationException` with source locations. **Out of scope
(deferred):** runtime configuration and any format other than TOML — see
[Deferred](#deferred).

## Where it sits

The loader is a satellite assembly between a project's config file and the core's
options; it depends on the core, never the reverse — an architecture test guards this
direction.

```mermaid
flowchart LR
    T["dialogue.toml"] --> L["TomlConfigurationLoader<br/>(DialogueDown.ConfigurationLoader)"]
    L --> O["CompilerOptions<br/>(DialogueDown.Configuration)"]
    O --> C["ScriptCompilerFactory /<br/>AddDialogueDown"]
    style L fill:#2d6,stroke:#0a0,color:#000
```

A consumer that wants file-backed config references
`DialogueDown.ConfigurationLoader`; a consumer that builds `CompilerOptions` in code
(or from another source) does not, and the core never takes a TOML dependency.

## Ubiquitous language

| Term                     | Meaning                                                                                                                |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| **Configuration loader** | The component that reads `dialogue.toml` into a `CompilerOptions`.                                                     |
| **Schema reader**        | An internal reader that owns one configuration concern: mode, speakers, or unmodeled Markdown.                         |
| **Structural key**       | A speaker key the schema defines directly: `name`, `id`, `tags`.                                                       |
| **Reserved key**         | Any other speaker key — a reserved tag (`default`, and future `voice`, …), validated against `ReservedTagNames.Known`. |
| **Tag shorthand**        | A custom tag written as a DSL-style string, `"name"` or `"name=value"`.                                                |
| **Handling override**    | A named unmodeled-Markdown kind whose built-in `keep` or `ignore` handling the project changes.                        |
| **Edge validation**      | Rejecting a malformed or invalid config here, before it reaches the compiler.                                          |

## The `dialogue.toml` schema

```toml
# dialogue.toml

[[speakers]]
name    = "Narrator"          # required
id      = "narrator"          # optional stable id
default = true                # a reserved tag (typed key) -> ReservedTag("default")
tags    = ["main", "mood=happy", { name = "a=b", value = "c" }]   # custom tags
```

A `[[speakers]]` entry maps to one `ConfiguredSpeaker`:

- **`name`** (required, non-empty string) → `Name`.
- **`id`** (optional string) → `Id`.
- **`tags`** (optional array) → `CustomTags`. Each element is either a **shorthand
  string** (`"name"` or `"name=value"`, split at the first `=`) or an **inline table**
  (`{ name = "…", value = "…" }`, `value` optional) — the escape hatch for a name that
  itself contains `=`. Both forms become `ConfiguredTag(name, value?)`.
- **every other key** is a **reserved tag**: a `true` boolean → a name-only
  `ConfiguredTag(key)`; a string → `ConfiguredTag(key, value)`. `false` (or omitted)
  contributes nothing. The key must be in `ReservedTagNames.Known`.

So `default = true` marks the default speaker exactly as the DSL's `##default` does,
and the author writes typed keys rather than tag strings.

A top-level `mode` key selects the settable compilation mode:

```toml
mode = "best-effort"
```

The `[markdown.unmodeled]` table maps unmodeled Markdown kinds to `keep` or
`ignore`. Omitted kinds remain absent from `CompilerOptions.UnmodeledMarkdown`,
so the core preserves their built-in defaults:

```toml
[markdown.unmodeled]
table      = "keep"
code-block = "ignore"
```

## Functionality checklist

- [x] `TomlConfigurationLoader.Load(path)` builds a `CompilerOptions` from a
      `[[speakers]]` array (with an internal `Parse(toml, sourceName)` for in-memory input).
- [x] A top-level `mode` maps to a settable `CompilationMode`; an absent mode
      keeps the default.
- [x] `[markdown.unmodeled]` maps each known kind to `keep` or `ignore`; omitted
      kinds remain absent so their built-in defaults survive.
- [x] Structural keys (`name`, `id`, `tags`) map to their `ConfiguredSpeaker` fields,
      each resolved by its semantic name, so a quoted key equals its bare form.
- [x] Custom `tags` accept a shorthand string (split at the first `=`) or an inline
      table (`{ name, value }`), for full DSL parity including a name containing `=`.
- [x] Every other key partitions into a **reserved tag** (boolean → name-only, string →
      valued), validated against `ReservedTagNames.Known`.
- [x] Edge validation rejects a missing or empty `name`, an empty `id`, a wrong-typed
      key, an unknown key, a reserved tag that is neither boolean nor string, an
      inline-table tag without a `name` or with an unknown field, a dotted key, and a
      second `default`.
- [x] A `DialogueConfigurationException` reports the message and source location
      (source, line, column), for both TOML syntax errors and schema violations.
- [x] Empty config yields `CompilerOptions.Default`; a config that sets only one
      concern leaves the others at their defaults.
- [x] An architecture test guards the direction: the core does not depend on the
      loader, and the loader depends only on the core (and Tomlyn).

## Interfaces and abstractions

| Type                                | Visibility | Responsibility                                                               | Collaborators                                      |
| ----------------------------------- | ---------- | ---------------------------------------------------------------------------- | -------------------------------------------------- |
| `TomlConfigurationLoader`           | public     | `Load(path)` / `Parse(text, source)` → `CompilerOptions`; composes readers   | parser and schema readers                          |
| `TomlDocumentParser`                | internal   | parses text into Tomlyn's `DocumentSyntax`, failing on a syntax error        | Tomlyn                                             |
| `ConfiguredModeReader`              | internal   | reads and validates the top-level compilation mode                           | `CompilationModes`, Tomlyn syntax                  |
| `ConfiguredSpeakerReader`           | internal   | maps and validates each `[[speakers]]` entry                                 | configuration model, Tomlyn syntax                 |
| `ConfiguredUnmodeledReader`         | internal   | reads `[markdown.unmodeled]` as handling overrides                           | `UnmodeledMarkdownNames`, Tomlyn syntax            |
| `TomlTables`                        | internal   | selects named regular or array tables consistently                           | `TomlKeys`, Tomlyn syntax                          |
| `TomlErrors`                        | internal   | creates a located configuration error at a syntax node                       | `TomlLocation`                                     |
| `DialogueConfigurationException`    | public     | a config error carrying a source location                                    | `ConfigurationSourceLocation`                      |
| `ConfigurationSourceLocation`       | public     | the source, line, and column of a config error                               | —                                                  |
| `TomlLocation`                      | internal   | maps a Tomlyn span to a `ConfigurationSourceLocation`                        | Tomlyn syntax                                      |

Internally the loader walks Tomlyn's syntax tree. The document parser isolates
syntax parsing; each schema reader maps one concern; `TomlTables`, `TomlKeys`,
`TomlErrors`, and `TomlLocation` keep the syntax-level mechanics consistent.
All remain internal behind `Load` and `Parse`.

## Key design decisions

### DD1 — A satellite assembly with Tomlyn, not a core dependency

The loader is its own project (`DialogueDown.ConfigurationLoader`) depending on the
core plus **Tomlyn**, so the engine-agnostic core never takes a TOML dependency (its
guiding constraint). Tomlyn is the de facto .NET TOML library — by the same author as
Markdig (already used here), used by the .NET SDK, permissively licensed, and its
parser yields **precise line/column diagnostics**, which is what edge validation
needs. The project is format-named-agnostic (`ConfigurationLoader`, not `.Toml`) since
TOML is the one decided format; the entry type `TomlConfigurationLoader` names the
format at the API.

### DD2 — Traverse Tomlyn's syntax tree, not a fixed POCO

A speaker's **reserved keys are open-ended** (`default`, later `voice`, …), and
the unmodeled-Markdown table has its own closed vocabulary, so a fixed POCO
would either lose unknown-key validation or mix unrelated schemas. The loader
therefore parses to Tomlyn's round-trippable `DocumentSyntax` and gives each
section to a focused reader. Every syntax node carries a native span, giving
precise line/column error locations. This trades a little traversal code for
control over partitioning and diagnostics.

### DD3 — Reserved as typed keys, custom as shorthand with a structured escape hatch

The schema honors the two-list tag model. **Reserved tags are typed keys**
(`default = true`), so a bad reserved name is caught at the edge against the shared
`ReservedTagNames.Known`, and a multi-word reserved name comes free from TOML key
quoting. **Custom tags are DSL-shorthand strings** (`"mood=happy"`, split at the first
`=`), so an author reuses the script syntax — which already covers multi-word names
and valued tags. For the one case shorthand cannot express — a tag **name** that
contains `=` — a custom tag may instead be an **inline table** (`{ name, value }`),
giving full parity with the DSL's quoted tags (TOML 1.1 allows the mixed array). The
loader maps both forms to `ConfiguredTag`, and the core's builder turns them into AST
tags.

### DD4 — Validate at the edge, fail with a location

The loader is where a config is proven well-formed, so it rejects everything
the compiler would otherwise mishandle — missing names, wrong types, unknown
keys or values, two defaults — as a `DialogueConfigurationException` carrying
the path, line, and column. `TomlErrors` centralizes that syntax-node-to-location
mapping; readers retain ownership of their domain-specific messages. The
compiler downstream can then trust its `CompilerOptions`.

## Error and boundary cases

| Case                                                          | Behavior                                                                 |
| ------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Malformed TOML                                                | `DialogueConfigurationException` from Tomlyn's diagnostic (line/column). |
| Missing or empty `name`                                       | `DialogueConfigurationException`.                                        |
| Wrong-typed key (`default` not bool, `tags` not string array) | `DialogueConfigurationException`.                                        |
| Unknown reserved key (not `name`/`id`/`tags`, not in `Known`) | `DialogueConfigurationException`.                                        |
| Inline-table tag without a `name`                             | `DialogueConfigurationException`.                                        |
| More than one `default = true`                                | `DialogueConfigurationException`.                                        |
| Unknown or non-settable `mode`                                | `DialogueConfigurationException`.                                        |
| Unknown unmodeled kind or handling                            | `DialogueConfigurationException`.                                        |
| Non-string unmodeled handling                                 | `DialogueConfigurationException`.                                        |
| `default = false` or omitted                                  | contributes no reserved tag.                                             |
| Empty file                                                    | `CompilerOptions.Default`.                                               |
| `Load` on a missing file                                      | the underlying IO exception (a usage error, not a config error).         |

## Integration

- **Core** (`DialogueDown`): unchanged — it takes a `CompilerOptions`. The loader
  reuses `CompilerOptions`, `ConfiguredSpeaker`, `ConfiguredTag`, and the now-public
  `ReservedTagNames`.
- **Loader** (`DialogueDown.ConfigurationLoader`): the new satellite; `TomlConfigurationLoader`
  produces a `CompilerOptions` a caller hands to `ScriptCompilerFactory.CreateDefault`
  or `AddDialogueDown`.
- **CLI / visualization**: config discovery and `--config` load through this
  edge before constructing the compiler and report.
- **Architecture**: a test asserts the core does not depend on the loader and the
  loader depends only on the core (and Tomlyn), guarding the satellite direction.

## Testability

- **Parsing**: valid TOML → the expected `CompilerOptions` (mode, speakers,
  unmodeled handling), with multi-line raw-string TOML so the input's shape is
  visible.
- **Validation**: each error case above gets a test asserting the thrown
  `DialogueConfigurationException` and its reported location.
- **Integration**: loader tests assert the composed `CompilerOptions`; core tests
  prove that both composition roots apply the configured speaker and
  unmodeled-Markdown settings to a real compile.
- **Architecture**: the dependency-direction test above (core ⊄ loader; loader → core
  only), extending the existing assembly-boundary suite.
- Construction goes through a small TOML test helper; the loader is stateless and
  unit-tested in isolation.

## Deferred

| Item                              | Note                                                                                                                |
| --------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Runtime configuration             | Belongs to the runtime component once its public settings exist.                                                    |
| Config formats other than TOML    | TOML is the decided format; the project name leaves room but no other loader is planned.                            |
| Duplicate-name detection at edge  | The speaker binder already reports speaker conflicts; edge de-duplication can follow.                               |
| Aggregate / collected diagnostics | Fails fast on the first error like every stage; collecting all config problems joins the planned diagnostics phase. |
