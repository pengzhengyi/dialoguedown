# Disabled Diagnostics

> [!NOTE]
> Status: **implemented**. How a writer turns a diagnostic off **by code**:
> a `[diagnostics]` table in `dialogue.toml` for the whole project, and
> `ddown compile --disable` for one run. A disabled diagnostic is still computed and
> carried, marked as disabled, so each surface decides how to show it. This note
> covers the core, the configuration loader, and the CLI; the report's controls are
> [planned](#planned-the-report).

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Where it sits](#where-it-sits)
- [The `dialogue.toml` table and the CLI option](#the-dialoguetoml-table-and-the-cli-option)
- [Examples](#examples)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)
- [Planned: the report](#planned-the-report)
- [Not built](#not-built)

## Goal and scope

Every `Info` and `Warning` is shown on every compile, whether or not the writer has
decided it does not apply to their project. A note that fires out of habit, such as
`DLG3002` in a project that nests choices deeply on purpose, buries the notes that
matter.

This component lets a writer disable an optional code for a project or a run, and
makes every existing surface honor it: the CLI's errata, summary, fix mode, and
fixable hint, and the report's diagnostics. The report's own controls for disabled
diagnostics (a muted Problems group, row actions that edit `dialogue.toml`, code
autocompletion in the Config tab) are the next component. Until then the report
hides disabled diagnostics.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Disable** | Turn a diagnostic code off for a project or a run. The verb in TOML (`disable`), on the CLI (`--disable`), and in code (`DisabledDiagnostics`). |
| **Disabled diagnostic** | A diagnostic whose code is disabled. It keeps its code, span, severity, message, and fixes, and carries `IsDisabled`. |
| **Optional code** | A code whose default severity is `Info` or `Warning`, so it can be disabled. An `Error` code is never optional. |
| **Enabled diagnostic** | Any diagnostic that is not disabled: what a surface shows by default. |

The word is *disabled*, never *ignored* or *suppressed*. *Ignored* already means
[ignored Markdown](Ignored%20Markdown%20Diagnostic.md), which `DLG1114` reports and a
writer could disable. *Suppressed* reads as a per-site comment in the script, which
this component does not offer.

The report also uses *disabled* for unavailable controls, such as a stage tab with no
artifact. The planned Problems group must word its rows so a disabled diagnostic is
not mistaken for a disabled control, for example "Disabled in dialogue.toml".

## Functionality checklist

- [x] `DiagnosticCatalog.All` lists every declared descriptor, and a public
      `DisabledDiagnosticCodes.Check` tells `CompilerOptions`, the loader, and the CLI
      whether a code can be disabled, and if not, why.
- [x] `CompilerOptions.DisabledDiagnostics`: a set of optional codes, empty by
      default, normalized to upper case, compared as a set.
- [x] `Diagnostic.IsDisabled` and `LocatedDiagnostic.IsDisabled`, set during the
      compile for every non-error diagnostic whose code is disabled.
- [x] `HasErrors`, the compile's outcome, the stage-boundary halt, and exit codes are
      unchanged.
- [x] `[diagnostics] disable = [...]` in `dialogue.toml`, with a located
      `DialogueConfigurationException` for every malformed shape, an unknown code,
      or an `Error` code.
- [x] `ddown compile --disable <code>`, repeatable and comma-separated, added to the
      configured set; an unknown or `Error` code is a usage error.
- [x] The CLI prints no disabled diagnostic; a line after the fixable hint counts
      them by code: `2 disabled: 1 DLG1114, 1 DLG3002`.
- [x] `--fix` ignores disabled diagnostics: it applies none of their fixes and counts
      none of them in the hint, the remaining count, or the "after fixing" list.
- [x] The report shows no squiggle, gutter marker, Problems row, or "Fix all" entry
      for a disabled diagnostic, and still colors ignored Markdown from a disabled
      `DLG1114`.
- [x] Guides: `[diagnostics]` in the configuration guide, `--disable` in the CLI
      guide, and which codes are optional in the error-code reference.

## Where it sits

The set travels with the other options. The compile marks each diagnostic as it is
reported, so every surface reads one flag and none re-derives the rule.

```mermaid
flowchart LR
    T["dialogue.toml<br/>[diagnostics] disable"] --> L["TomlConfigurationLoader"]
    L --> O["CompilerOptions<br/>DisabledDiagnostics"]
    A["--disable"] -->|"union"| O
    O -->|"ForCompilationSession()"| C["CompilationSessionOptions<br/>mode and disabled codes"]
    C --> S["ScriptCompiler → CompilationSession<br/>DisablingDiagnosticSink"]
    S --> R["LocatedDiagnostic.IsDisabled"]
    R --> E["CLI: errata, summary, --fix"]
    R --> P["Report: LSP projection<br/>(skips disabled)"]
    R --> K["Report: ignored-Markdown tokens<br/>(reads all)"]
```

## The `dialogue.toml` table and the CLI option

```toml
# dialogue.toml
[diagnostics]
disable = ["DLG3002", "DLG1114"]   # any order; duplicates collapse
```

```bash
ddown compile scene.dialogue.md --disable DLG1003            # adds to the configured set
ddown compile scene.dialogue.md --disable DLG1003,DLG3004    # same as two --disable options
```

`--disable` is repeatable and also takes a comma-separated list, as Ruff's
`--extend-ignore` does.

**Precedence: the configured set ∪ `--disable`.** `CompileCommand` builds the union
after resolving the configuration, the way it applies `--mode`:
`options with { DisabledDiagnostics = options.DisabledDiagnostics.Union(settings.DisabledCodes) }`.
The option only adds, like Ruff's `--extend-ignore`, so a run cannot re-enable a
code the project disabled.

A code must name an entry in the diagnostic catalog, matched case-insensitively.

## Examples

One script, compiled under different `disable` lists, shows what a writer sees. The
output is the piped, one-line form; a terminal shows the same diagnostics as Errata
blocks with the same summary lines. The script raises three diagnostics:

```markdown
# The workshop

Alice: Which way?

=> The market

---

- Ask about the wrench
    - Ask again
        - And again
            - One more time
                Bob: Top shelf.

# The market

Merchant: Wares!
```

| Line | Code | Severity | Why | Fix |
| --- | --- | --- | --- | --- |
| 5 | `DLG1113` | warning | `=>` with no link after it | yes |
| 7 | `DLG1114` | info | `---` is left out of the script | no |
| 12 | `DLG3002` | warning | a choice nested four levels deep | no |

**Nothing disabled, or nothing that matches** (`disable = ["DLG3004"]`). No count is
printed for zero:

```text
scene.dialogue.md(5,1): warning DLG1113: `=>` makes a jump only when a link follows it. With no link here it is read literally, staying as the characters "=>". If you meant to jump, add a target: `=> [The market](#the-market)`. If you meant the characters, escape the arrow: `\=>`.
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg1113
scene.dialogue.md(7,1): info DLG1114: This divider is not dialogue, so the compiler left it out of the script. That is expected for notes and diagrams; write it as dialogue if it should be spoken.
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg1114
scene.dialogue.md(12,13): warning DLG3002: This branch reaches choice nesting level 4; the recommended maximum is 3. Consider moving this branch into a new scene and jumping to it instead.
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg3002
2 warnings, 1 info
1 fixable with --fix
```

**Some match** (`disable = ["DLG1114", "DLG3002"]`, or `DLG1114` in the file and
`--disable DLG3002`). The severity summary counts only what is shown, and a last line
counts what is hidden, by code:

```text
scene.dialogue.md(5,1): warning DLG1113: `=>` makes a jump only when a link follows it. With no link here it is read literally, staying as the characters "=>". If you meant to jump, add a target: `=> [The market](#the-market)`. If you meant the characters, escape the arrow: `\=>`.
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg1113
1 warning
1 fixable with --fix
2 disabled: 1 DLG1114, 1 DLG3002
```

**The fixable one matches** (`disable = ["DLG1113"]`). The fixable hint goes with it,
and `--fix` leaves line 5 alone:

```text
scene.dialogue.md(7,1): info DLG1114: This divider is not dialogue, so the compiler left it out of the script. That is expected for notes and diagrams; write it as dialogue if it should be spoken.
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg1114
scene.dialogue.md(12,13): warning DLG3002: This branch reaches choice nesting level 4; the recommended maximum is 3. Consider moving this branch into a new scene and jumping to it instead.
  for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg3002
1 warning, 1 info
1 disabled: 1 DLG1113
```

**All match** (`disable = ["DLG1113", "DLG1114", "DLG3002"]`). Only the count prints:

```text
3 disabled: 1 DLG1113, 1 DLG1114, 1 DLG3002
```

Every case exits `0`. A mistake in the list stops the compile instead:

```text
dialogue.toml(2,12): Unknown diagnostic code 'DLG302'. Check the code against the error-code reference: https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html
dialogue.toml(2,12): DLG2001 is an error and cannot be disabled; only info and warning codes can. Fix the problem it reports instead.
```

The first comes from `disable = ["DLG302"]` and the second from `disable = ["DLG2001"]`,
each exiting `65`. The same codes given to `--disable` are usage errors, exiting `64`,
with the same sentence after the option and the code as typed:

```text
--disable DLG2001: DLG2001 is an error and cannot be disabled; only info and warning codes can. Fix the problem it reports instead.
```

## Interfaces and abstractions

| Type | Visibility | Responsibility |
| --- | --- | --- |
| `DiagnosticCatalog.All` | internal | Every descriptor the catalog declares, read from its public static fields on first use. |
| `DisabledDiagnosticCodes.Check(code)` | public | Whether a project may disable a code, as a `DisabledCodeCheck(Code, Problem)` with `IsValid`: the code, upper-case when the compiler reports it, and the sentence every surface reports when it cannot be disabled. Descriptors stay internal. |
| `DiagnosticDocumentation` | public | The Error codes page (`ReferencePage`) and a code's anchor on it (`UrlFor`), cited by the check and the CLI renderer. |
| `CompilerOptions.DisabledDiagnostics` | public | An `ImmutableHashSet<string>` with an ordinal comparer, holding upper-case codes; validated on `init`. |
| `CompilerOptions(mode, speakers, unmodeled, disabledDiagnostics)` | public | An overload that adds `disabledDiagnostics`; the three-argument one stays. |
| `CompilationSessionOptions` | internal | What each compilation session reads from the options: the mode and the disabled codes, built by `CompilerOptions.ForCompilationSession()`. |
| `DisablingDiagnosticSink` | internal | An `IDiagnosticSink` decorator over a set of code strings: marks a non-error diagnostic whose code is in the set, then forwards it. |
| `Diagnostic.IsDisabled`, `LocatedDiagnostic.IsDisabled` | internal, public | The mark, an `init` property on both (not a positional parameter), included in equality. |
| `ConfiguredDiagnosticsReader` | internal | Reads and fully validates `[diagnostics]` from the TOML syntax tree. |
| `CompileSettings.Disable`, `DisabledCodes` | internal | The `--disable` values as given, and the codes in them, split at commas and trimmed; `Validate` checks each with `DisabledDiagnosticCodes.Check`. |

## Key design decisions

### D1 — A code is the unit, and only an optional code can be disabled

Every tool in the prior art disables by code, never by message or category alone.
An `Error` means the compile cannot produce a correct playbook, so disabling one
would let a broken script pass. C# `NoWarn` and Rust's hard errors draw the same
line.

Whether a code is optional follows its descriptor's **default** severity. No
reporting site overrides a severity today, but `Diagnostic` allows it, so the sink
also checks the diagnostic's own severity and never marks an error. A future
severity map (see [Not built](#not-built)) would revisit this rule.

Because no disabled diagnostic is ever an error, `HasErrors`, the compile's outcome,
the stage-boundary halt, and the exit code need no change.

### D2 — Mark, do not drop

A disabled diagnostic is still computed and carried, like Roslyn's
`Diagnostic.IsSuppressed`. Two consumers need that:

- the report's planned Problems group lists disabled diagnostics so a writer can
  re-enable one;
- the editor colors ignored Markdown from `DLG1114`'s spans
  ([Unmodeled Markdown Highlighting](../visualization/editor/Unmodeled%20Markdown%20Highlighting.md)
  D2), so dropping a disabled `DLG1114` would silently remove the coloring.

The mark is part of equality on both `Diagnostic` and `LocatedDiagnostic`. Unlike
`Fixes`, which only suggests a repair, the mark changes how a consumer must show the
diagnostic, so a consumer that caches by equality must see it change, and a test
that compares diagnostics catches a marking regression. It is an `init` property, not
a positional parameter, so `LocatedDiagnostic`'s deconstruction does not break. No
existing comparison changes: without a disabled set, every diagnostic is enabled, as
before.

### D3 — Mark in a sink decorator, during the compile

`ScriptCompiler` takes a `CompilationSessionOptions`, the mode and the set together,
which each composition root builds with `options.ForCompilationSession()`, the twin of
`ForSemanticAnalyzer()`. The two travel as one value because the six stages plus a
separate mode and set would be eight parameters, and Sonar's S107 caps a core
constructor at seven: the limit is set in `src/DialogueDown/SonarLint.xml` and is an
error for the core through `.editorconfig`.

`CompilationSession.Start(source, options)` always wraps the mode's sink in a
`DisablingDiagnosticSink`, which forwards every diagnostic unchanged when the set is
empty. The decorator is outermost, so it marks before the bag (or the fail-fast sink)
sees the diagnostic. It takes a set of strings, not `CompilerOptions`, so the
`Diagnostics` namespace stays a foundation leaf.

This gives one marking site for every composition root, and reporting sites stay
unaware, as they are of the mode
([Diagnostics and Validation](Diagnostics%20and%20Validation.md) D6). Marking once
while `ScriptCompiler` assembles the result would work too, but it would have to be
repeated on each result path (success and each failure), while the sink sees every
diagnostic exactly once. Marking in the CLI would leave the library and the report
to re-derive the rule.

### D4 — A flat set on `CompilerOptions`

`DisabledDiagnostics` sits beside `Mode` and `UnmodeledMarkdown`
([Configuration](../configuration/Configuration.md) D6). A future severity map
would be a sibling property, not a reshaping. The set is stored upper-case with an
ordinal comparer, through a backing field marked `[IgnoreEquality]` and a property
compared with `[UnorderedEquality]`, the pattern `UnmodeledMarkdown` uses. Because `init`
normalizes the codes to upper case, the getter returns a new set, not the instance
assigned.

`CompilerOptions` validates the set on `init` and throws `ArgumentException` for an
unknown or `Error` code, carrying the check's sentence. A code-level consumer that
passes one is misusing the API, which the [Error Model](../core/Error%20Model.md)
routes to the standard .NET channel.
The loader validates first and reports its own located error, so a writer never sees
the `ArgumentException`, and the served report, which catches only
`DialogueConfigurationException` when it reloads `dialogue.toml`, never crashes on one.

### D5 — One public check, not wider internals

`CompilerOptions`, the loader, and the CLI must each check a code against the catalog,
which is internal. Rather than add an `InternalsVisibleTo` for the loader, the core
exposes one static check, `DisabledDiagnosticCodes.Check(code)`. It returns a
`DisabledCodeCheck`: the code, upper-case when the compiler reports it, and a
`Problem` sentence when it cannot be disabled. Each caller reports that sentence and
adds only where the code was written: `CompilerOptions` throws it as an
`ArgumentException`, the loader places it at the code in `dialogue.toml`, and the CLI
puts `--disable CODE:` before it. So the rule and its wording live in one place, and
the check's tests cover every case once.

No caller needs a code's title or category, so the catalog has no other public view.
The check reads `DiagnosticCatalog.All`, which finds the descriptors from the
catalog's public static fields, so declaring a descriptor is enough to list it; a test
checks that no two share a code.

Codes match case-insensitively, so `dlg3002` and `DLG3002` are one code.

### D6 — Strict validation at the edge

An unknown or `Error` code in `disable` is a located `DialogueConfigurationException`,
as every other config problem is
([Configuration Loader](../configuration/Configuration%20Loader.md) D5). A typo such
as `DLG302` that silently does nothing is the failure this feature most needs to
avoid, and the lenient alternative, a warning like Rust's `unknown_lints`, needs a
warning channel the loader does not have. On the CLI the same check is a usage error
(exit `64`).

The cost is that changing the catalog can break a project's config on upgrade:
promoting a code to `Error`, or a config naming a code newer than the installed
`ddown`. The [Error Model](../core/Error%20Model.md) already never renumbers or
repurposes a code; promoting one to `Error` becomes a breaking change that the
changelog calls out.

The reader also rejects any other spelling of the root `diagnostics` key, such as an
inline table, a dotted key, an array of tables, or a nested table like
`[diagnostics.style]`, rather than silently ignoring it, since each would leave the
codes enabled. `[markdown.unmodeled]` has no such check. A misspelled table
name such as `[diagnostic]` is an unrelated root key, which the loader ignores for
forward compatibility.

### D7 — The CLI hides disabled diagnostics and counts them by code

`ErrataRenderer` filters disabled diagnostics before anything else, so neither the
blocks, nor the severity summary, nor the fixable hint sees them. When any diagnostic
was disabled, a last line counts them by code, in code order:
`2 disabled: 1 DLG1114, 1 DLG3002`. It prints even when every diagnostic was
disabled, and a compile with nothing to show and nothing disabled still prints
nothing. A zero count is never printed. The fix run's remaining count leaves disabled
diagnostics out. See [Examples](#examples).

Compilers and linters rarely count what was disabled: ESLint, rustc, tsc, MSBuild,
and flake8 hide it silently. Test runners do count deliberate exclusions, as in
the `2 skipped` that pytest prints (and omits at zero), and Ruff counts hidden fixes on a line of its own
after its fixable hint. A disable is a decision a writer should not forget, so the CLI
counts it like a skip. The per-code breakdown, written count first like Ruff's and
flake8's `--statistics`, shows which disabled codes actually fired. No
`--show-disabled` option is built.

### D8 — Fix mode respects the disabled set

`FixApplier` drops disabled diagnostics before choosing preferred fixes, so they are
not candidates at all, rather than recorded as skipped. A run whose only fixable
diagnostics are disabled therefore prints no fix section.
`FixApplication.NewDiagnostics` leaves them out of the "after fixing" list too. Rewriting text the writer chose not to hear about would surprise
them. To fix a code the project disabled, a writer removes it from `dialogue.toml`;
the CLI cannot re-enable it for one run.

### D9 — `--disable` is a `compile` option only

Like `--mode`, it tunes one run. `visualize` honors the configured set, and a writer
changes it in the report through `dialogue.toml`. With `--emit dot`, `--disable` is
accepted and has no effect, since that path prints no diagnostics.

### D10 — Until the report component, the report hides disabled diagnostics

`DiagnosticProjection` skips disabled diagnostics. Because the report's squiggles,
gutter markers, Problems rows, and "Fix all" with its counts all read that payload,
none of them shows a disabled diagnostic. `SemanticTokenProjection` keeps reading
every located diagnostic, so ignored-Markdown coloring survives. The report already
reloads `dialogue.toml` through the loader and applies options with `with`, so the
set reaches it with no other change. The next component replaces the skip with a
disabled group.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| No `[diagnostics]` table, or `disable = []` | Nothing disabled; the options equal `CompilerOptions.Default` when nothing else is set. |
| Only `[diagnostics]` set | Loaded; the loader's "nothing set → `Default`" shortcut also checks the set. |
| `disable` is not an array, or holds a non-string | Located `DialogueConfigurationException`. |
| An unknown or malformed code (`DLG12`, `DLG302`) | Located `DialogueConfigurationException`, naming the code. |
| An `Error` code | Located `DialogueConfigurationException`, saying errors cannot be disabled. |
| An unknown key in `[diagnostics]` | Located `DialogueConfigurationException`, as in `[markdown.unmodeled]`. |
| `diagnostics` as an inline table, dotted key, or array of tables | Located `DialogueConfigurationException`. |
| `[diagnostic]` (misspelled) | Ignored, like any unrelated root key. |
| A duplicate code, or one code in two cases | One entry. |
| `--disable` with an unknown or `Error` code | Usage error, exit `64`. |
| `--disable ""` or `--disable ","` (no code at all) | Usage error, exit `64`. |
| `--disable dlg1003` or `--disable " DLG1003 "` | Accepted as `DLG1003`. |
| `--disable DLG1003,` (trailing comma) | The empty entry is dropped; `DLG1003` is disabled. |
| An empty or whitespace string in `disable` | Located `DialogueConfigurationException`, as a malformed code. |
| `" DLG1003 "` in `disable` | Located `DialogueConfigurationException`; TOML values are taken as written, apart from case. |
| `--disable` naming a code the config already disables | No effect. |
| `--disable` with `--emit dot` | Accepted; no effect. |
| A disabled code that never fires | Nothing reported; unused-disable reporting is not built. |
| Only disabled diagnostics | Only the `N disabled: …` line; exit `0`. |
| A disabled code that matches nothing | No `0 disabled` line. |
| Only disabled diagnostics carry fixes, under `--fix` | No fix section; the output is a plain compile's. |
| `CompilerOptions` built in code with an unknown or `Error` code | `ArgumentException`. |

## Testability

- `DiagnosticCatalog.All` lists descriptors with unique codes.
- `DisabledDiagnosticCodes.Check` owns every case: an info or warning code in any
  case, an error code, an unknown, empty, or untrimmed code, and null. Each caller's
  tests check only what it adds.
- `CompilerOptions`: empty by default; codes normalize; equality ignores order and
  case; an unknown or `Error` code throws with the check's sentence; the
  three-argument constructor still works; `ForCompilationSession()` carries the mode
  and the set.
- `DisablingDiagnosticSink`: marks a code in the set, forwards the rest unchanged,
  and never marks an error, even one with an overridden severity.
- Composition roots: a real compile with a disabled code marks it, keeps its span and
  fixes, and leaves `HasErrors` unchanged, through both `ScriptCompilerFactory` and
  `AddDialogueDown`.
- Loader: raw-string TOML to the expected set, and one test per error case asserting
  the location; an invalid code is reported with the check's sentence, never as an
  `ArgumentException`.
- CLI through `CommandAppTester`: `--disable` unions with the config; each parsing
  edge case; an invalid code exits `64`; disabled diagnostics are not printed.
  `ErrataRenderer` on a `TestConsole` for the summary, the hint, and the remaining
  count. Fix mode: disabled fixes are not applied, and a run with only disabled
  fixes prints no fix section.
- Report: `DiagnosticProjection` skips a disabled diagnostic, a served document's
  payload carries none, and `SemanticTokenProjection` still colors a disabled
  `DLG1114`.

## Planned: the report

The next component gives the report its own controls, through the Config tab's
existing TOML editing, which can already create the file in place:

- the Problems panel lists disabled diagnostics in a muted group, or behind a
  filter;
- an enabled row offers **Disable DLG3002 in dialogue.toml**, a disabled row offers
  **Enable**, and either recompiles;
- the Config tab autocompletes codes in `disable` from the catalog's descriptors,
  which the visualization reads through its existing `InternalsVisibleTo`, carried to
  the client in a new configuration-report field, like the reserved tags.

It will be a section of this note.

## Not built

| Knob | Why not now |
| --- | --- |
| A severity map (`DLG3002 = "error"`) | A list is the smallest thing that works. A map can join it later as a sibling (`[diagnostics.severity]`) without breaking `disable`, once promoting a warning to an error that fails the compile is designed. |
| Inline suppression in a script (`<!-- ddown-disable-next-line -->`) | The front end discards comments today; a follow-up, with unused-suppression reporting. |
| Per-file scope in `dialogue.toml` | A follow-up; the CLI resolves one configuration per run. |
| Re-enabling a configured code from the CLI | No known need; the option only adds. |
| Accepting a well-formed but unknown code | Strict validation (D6) catches typos; the cost is a config naming a newer code fails on an older `ddown`. |
| `--show-disabled` | The summary count covers awareness; the report covers browsing. |
