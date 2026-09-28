# CLI Diagnostic Rendering

> [!NOTE]
> Status: **implemented**. How `ddown compile` shows the compiler's diagnostics:
> a public located view projected once in the engine, rendered as rich Errata
> blocks on a terminal or greppable one-liners elsewhere, a data-error exit code,
> and a `--mode` option.

## Table of contents

- [Ubiquitous language](#ubiquitous-language)
- [Output](#output)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Located diagnostic** | A diagnostic resolved to a line/column and a final message — the public unit a surface renders. |
| **Errata** | The CLI's rendering of one compile's located diagnostics, via the [Errata](https://github.com/spectreconsole/errata) library or the one-line fallback, plus a summary. |
| **`LineMap`** | The value that turns a source offset into a one-based line and column. |
| **`file(line,column)`** | The location format shared with configuration errors (`ConfigurationSourceLocation`). |

## Output

Non-interactive (piped, CI):

```text
scene.dialogue.md(3,27): warning DLG1113: `=>` makes a jump only when a link follows it. …
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg1113
scene.dialogue.md(5,1): error DLG2001: Two scenes resolve to the same anchor '#the-workshop'. …
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg2001
1 error, 1 warning
1 fixable with --fix
```

Interactive terminals get an Errata block per diagnostic — the source line, a
colored underline of the exact range, and a header such as `syntax warning [DLG1113]`
— with the same summary. Diagnostics are sorted by position, then code; errors are
red, warnings yellow, info cyan. A clean compile prints nothing. A `--fix` run adds a
section after the diagnostics, whose grammar is the
[fix mode](../cli/Compile%20CLI%20-%20Fix%20Mode.md) note.

| Outcome | Exit |
| --- | --- |
| No errors (warnings or info allowed) | `0` (`Success`) |
| The script or `dialogue.toml` has errors | `65` (`DataError`, sysexits `EX_DATAERR`) |
| Bad arguments, or an unknown `--mode` | `64` (`UsageError`) |
| Unexpected failure | `1` (`Error`) |

## Interfaces and abstractions

| Type | Responsibility |
| --- | --- |
| `LineMap` (engine, internal) | Precompute line starts; `Locate(offset)` → `LinePosition`. |
| `LinePosition` (public) | One-based `(Line, Column)`; `ToString()` is `line,column`. |
| `LocatedDiagnostic` (public) | `Code`, `Severity`, `Category`, `Message`, `Start`, `End`, `StartOffset`, `EndOffset`, and `Fixes`. |
| `CompilationResult.LocatedDiagnostics` (public) | The projection, built on first access and cached. |
| `ErrataRenderer` (CLI) | Render diagnostics, summary, fixable hint, and a fix run's section to an `IAnsiConsole`. |
| `DiagnosticDocumentation` (CLI) | Map a code to its hosted error-code anchor (`…/error-codes.html#dlg<code>`). |
| `CompileCommand` (CLI) | Compile, render, choose the exit code; parse `--mode`. |

## Key design decisions

### D1 — A small public view; internals stay internal

`Diagnostic`, `SourceSpan`, and `DiagnosticDescriptor` stay internal and free to
change. Consumers depend on `LocatedDiagnostic`, whose `DiagnosticSeverity` and
`DiagnosticCategory` are public with explicit numeric values. The character offsets
are plain integers beside the line/column, so the renderer can underline the exact
range and an editor can select it.

### D2 — The engine composes text and locations; the CLI presents

Filling the message format and locating the span happen once, in the engine's
projection, under `CultureInfo.InvariantCulture`, so the CLI and the report editor
show identical text and positions. The CLI adds layout, color, sorting, and the
summary, and is the only project that depends on Errata.

### D3 — One `file(line,column)` format

The engine compiles one source string and does not know its path, so the CLI
supplies the file. This is the MSVC/Roslyn/TypeScript shape and matches
configuration errors. When multi-source compilation exists, the located diagnostic
gains a source identifier and the format stays the same.

### D4 — `LineMap` semantics

- Valid offsets are `0..source.Length` inclusive: a span is half-open, so its end
  may equal `Length`, and a zero-width span (a filled-in default speaker) may sit
  there. An offset outside that range is a broken compiler span and throws.
- Lines and columns are one-based, in UTF-16 code units (the LSP convention).
- `\n` ends a line; `\r` is an ordinary character on its line.
- `"abc"` → offset 3 is `(1,4)`; `"abc\n"` → offset 4 is `(2,1)`; `""` → offset 0 is
  `(1,1)`.

### D5 — Exit codes follow sysexits

Every "your input is wrong" outcome — script or configuration — shares `65`, so a
script can branch on it.

### D6 — `--mode` offers the two collecting modes

`--mode <stage-boundary|best-effort>` overrides the resolved mode only when given, so
a `mode` in `dialogue.toml` is not clobbered. Fail-fast is not offered: it throws an
internal exception instead of returning diagnostics to render. What each mode does is
[Compilation modes](Diagnostics%20and%20Validation.md#compilation-modes).

### D7 — Errata for the rich path, a one-liner as fallback

Errata (MIT, Spectre.Console-based, modeled on Rust's Ariadne) renders snippets and
underlines from a source and labeled ranges. Its header holds one label, so it shows
category and severity together. The one-liner exists on its own merits for CI and
grep, so the design does not depend on Errata; Spectre's interpolated markup escapes
message text, so a diagnostic cannot inject console markup.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| Zero-width span | A single caret position. |
| Multi-line span | Start and end are both carried; the one-liner shows the start. |
| Offset past `Length` | `LineMap` throws; never clamped. |
| Message containing `[` or `]` | Escaped, not interpreted as markup. |
| No diagnostics | Nothing printed; exit `0`. |
| Warnings or info only | Printed in their color; exit `0`. |

## Testability

- `LineMap`: offsets across lines, `\r\n`, empty source, the end positions above, and
  the throw.
- Projection: messages are the formats filled under the invariant culture, at the
  right positions.
- `ErrataRenderer` on a Spectre `TestConsole`: sorted one-liners, colors, the summary,
  escaping, silence for a clean report, the fix section and hint.
- `CompileCommand` through `CommandAppTester`: clean → `0`; errors → `65`;
  warnings → `0`; `--mode` threads through or inherits; an unknown mode → `64`.
