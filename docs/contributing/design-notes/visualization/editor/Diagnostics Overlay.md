# Diagnostics Overlay

> [!NOTE]
> Status: **implemented**. The Source editor underlines each compiler diagnostic, marks its line in
> the gutter, and explains it on hover with a link to its error code, from an LSP-shaped projection
> in the report payload. It is the web half of the editor seams in
> [Diagnostics and Validation](../../diagnostics/Diagnostics%20and%20Validation.md).

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Payload shape](#payload-shape)
- [Interfaces and responsibilities](#interfaces-and-responsibilities)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)
- [Out of scope](#out-of-scope)

## Goal and scope

Show the compiler's diagnostics where the writer is looking: a squiggle on the range, a marker in
the gutter, and a tooltip with the message and a link to the
[error code](../../../../guide/error-codes.md). The same diagnostics feed the Problems panel and the
status-line counts ([Chrome and Layout](../session/Chrome%20and%20Layout.md#problems-panel)), and
their fixes are in [Diagnostic Quick Fixes](./Diagnostic%20Quick%20Fixes.md).

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Diagnostic** | The compiler's `LocatedDiagnostic`: code, severity, message, and source range. |
| **LSP diagnostic** | Its projection into the Language Server Protocol shape: zero-based `range`, integer `severity` (1–4), `code`, `message`, `source`. |
| **Overlay** | The editor rendering: squiggles, gutter markers, tooltips, via `@codemirror/lint`. |
| **Co-located diagnostics** | Diagnostics with the same start position; an **exact collision** also shares the end. |
| **Collision segment** | A span CodeMirror splits out because several diagnostics cover it. |
| **Dominant severity** | The most severe diagnostic on a segment or line: Error, Warning, Info, Hint. |
| **Canonical order** | The one deterministic order every surface receives. |

## Payload shape

```ts
export interface LspDiagnostic {
    range: LspRange;       // zero-based
    severity: LspSeverity; // 1 Error | 2 Warning | 3 Information | 4 Hint
    code: string;          // e.g. "DLG2001"
    message: string;
    source: string;        // "dialoguedown"
    fixes?: LspFix[];      // see Diagnostic Quick Fixes
}

export interface Report {
    diagnostics?: LspDiagnostic[];
}
```

## Interfaces and responsibilities

| Type | Responsibility |
| --- | --- |
| `DiagnosticProjection` (.NET) | Maps `LocatedDiagnostic` to `LspDiagnostic`: one-based to zero-based, severity to its protocol number. Pure. |
| `LspSeverity` (.NET) | The protocol numbers; a property-level `JsonNumberEnumConverter` keeps it an integer on the wire, since the report serializer writes other enums as strings. |
| `CompilationVisualizer.BuildContent` | Projects `result.LocatedDiagnostics` for complete and halted compiles alike. |
| `diagnostic-order.ts` | The canonical comparator and copy-sort. |
| `diagnostics-overlay.ts` | Converts payload diagnostics to `@codemirror/lint` values (range to offset, severity, tooltip with doc link, fix actions). |
| `source-view.ts` | Mounts the lint gutter; `setDiagnostics` pushes new diagnostics without rebuilding the editor. |
| `app.ts` | One fan-out: canonical order once, then the overlay, the Problems panel, and the counts. |

## Key design decisions

### D1 — An LSP-shaped projection, not a bespoke payload

The payload carries exactly the LSP shape, so a language server could publish the same values and
the editor would consume them unchanged. The projection lives in `DialogueDown.Visualization`,
its only consumer, and depends only on the core diagnostic model.

### D2 — `@codemirror/lint` renders it

The official CodeMirror package provides squiggles, `lintGutter`, and tooltips for diagnostics from
any source, and is what an LSP client would forward into. Diagnostics are pushed imperatively,
because they come from the server's compile, not a client linter.

### D3 — The tooltip is a viewport popover

The Source pane keeps `overflow: hidden` for its resizable split, so the tooltip layer mounts under
`document.body` with fixed positioning. CodeMirror then places it above or below the range inside
the viewport, it overlays the tab bar and preview without clipping, and its link stays clickable.
Messages wrap at a compact width rather than truncating.

### D4 — Diagnostics ride the existing payload and live channel

No new transport: the static export, `/api/document`, `/api/save`, and the hot-reload push all
serialize the same report. The app pushes on load, on a View reload, and after each save; a clean
compile clears the overlay.

### D5 — The tooltip links to the error code

A **more information** link opens the code's `#dlg<code>` entry on the error-codes page — the anchor
the [CLI](../../diagnostics/CLI%20Diagnostic%20Rendering.md) links to as well. The client builds the
URL from the code.

### D6 — Stage-boundary compilation stays

The visualizer compiles to the stage boundary, so a halted compile shows the diagnostics from the
stages it reached, and later tabs show as unavailable
([Unavailable Stage Tabs](../report/Compilation%20Visualization.md#unavailable-stages)).

### D7 — One dominant marker; every diagnostic kept in details

An editor line has room for one gutter icon, and three squiggle colors on the same pixels read as
noise. CodeMirror's `maxSeverity` already picks each collision segment's squiggle and each line's
marker; that stays. The marker is a summary, not a filter: hovering lists every active diagnostic,
and the Problems panel keeps one row each, because a warning may explain how to repair an error.

### D8 — One canonical order for every surface

LSP defines `diagnostics` as an array with no order, and the compiler reports in pass order, so the
client orders them once, before the fan-out:

```text
start line → start character → severity (Error, Warning, Info, Hint)
→ end line → end character → code → message
```

Position stays first, so the Problems panel still walks the script top to bottom; severity only
breaks ties at the same start. Code and message compare ordinally, so a permuted input renders
identically. The comparator returns a sorted copy and never mutates the payload; an unknown
severity ranks with errors. Because `Array.prototype.sort` is stable and CodeMirror sorts only by
`from` and `to`, exact collisions keep this order in the tooltip. Partially overlapping ranges keep
CodeMirror's geometry order in the range tooltip; replacing it would mean a custom tooltip and
segmentation for little gain. Exact collisions are not grouped behind a disclosure row.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| Zero-width span | A collapsed range; still squiggled, listed, and counted. |
| Range past the buffer, or buffer edited since the compile | Clamped; ranges map until the next compile. |
| Clean compile | Overlay, gutter, list, and counts clear. |
| Error, warning, and info on the exact same range | One red squiggle and marker; tooltip and Problems rows read Error, Warning, Info. |
| Several diagnostics on one line at different positions | One severest marker; Problems rows stay left to right. |
| Same range and severity | Code, then message. |
| Hint | Kept as Hint in the editor; counted and styled as Info in the Problems panel. |
| Diagnostic near a viewport edge | The tooltip flips or is constrained to stay on screen. |

## Testability

- **.NET** (`DiagnosticProjectionTests`, `LspSeverityTests`): zero-based ranges, integer severity,
  zero-width and multi-line spans; the payload carries `diagnostics` and an empty array when clean.
- **Vitest:** conversion to editor diagnostics with offsets, severities, and the doc link; every
  permutation of an exact-range Error/Warning/Info set sorts identically; position beats severity;
  end, code, and message break ties; the payload is not mutated; counts include every diagnostic.
- **Browser, static:** a fixture with an exact collision supplied Info, Warning, Error; a nested set;
  and a zero-width Hint. It asserts one error marker, tooltip order Error, Warning, Info, matching
  Problems rows, the counts, and an axe pass in both themes, then repeats with the input reversed.
- **Browser, live:** an edit that introduces an error shows the overlay after save; fixing it clears
  it.

## Out of scope

- A language server and its client transport.
- Compile-as-you-type; the overlay refreshes on recompile.
- Grouping, hiding, or compiler-side prioritization of co-located diagnostics.
