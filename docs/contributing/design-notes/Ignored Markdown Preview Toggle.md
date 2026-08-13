# Ignored Markdown Preview Toggle

> [!NOTE]
> Status: **approved — in progress**. Extends
> [Unmodeled Markdown Highlighting](./Unmodeled%20Markdown%20Highlighting.md) with one global
> Preview control: show every ignored construct in full, or collapse all of them while preserving
> where and what they were.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Functionality checklist](#functionality-checklist)
- [Writer-facing behavior](#writer-facing-behavior)
- [Architecture](#architecture)
- [Interfaces and responsibilities](#interfaces-and-responsibilities)
- [Key design decisions](#key-design-decisions)
- [Boundary cases](#boundary-cases)
- [Testability](#testability)

## Goal and scope

Ignored Markdown remains visible in Preview by default so a writer can inspect authoring aids and
accidental omissions. A document with several tables, diagrams, or dividers can nevertheless
spend most of its Preview space on content that never becomes dialogue.

This component adds one global toggle for the current Preview. It defaults to expanded; when
collapsed, every ignored block becomes a one-line semantic summary and every ignored inline
becomes a closed-eye chip at its original sentence position.

**In scope:** the fixed Preview footer, global expanded/collapsed state, compact block and inline
representations, persistence across file switches in one served report, hot-reload behavior, and
a real configured inline-ignore demo through
[the configuration loader](./Configuration%20Loader.md).

**Out of scope:** changing the handling policy, hiding ignored text in the Source editor,
per-region expansion, or storing a different state per script.

## Functionality checklist

- [ ] Always render a Preview footer matching the Source editor's `#END` footer height.
- [ ] Show the ignored-region count and current Preview state.
- [ ] Disable the action when the count is zero.
- [ ] Default to expanded, preserving current behavior.
- [ ] Collapse every ignored block to `Kind · N lines` at its original position.
- [ ] Collapse every ignored inline to one closed-eye chip with a source tooltip.
- [ ] Keep Source editor content visible and dimmed in either state.
- [ ] Persist one view preference across file switches and hot reloads.
- [ ] Reapply the state when a recompile changes the ignored regions.
- [ ] Verify an ignored inline through a real `dialogue.toml` override.

## Writer-facing behavior

The two panes end with matching compiler-owned footers:

| Source footer | Preview footer |
| --- | --- |
| `∞  End  #END` | `closed-eye  3 ignored  shown in Preview  [hide]` |

When collapsed, the footer reads `3 ignored · hidden in Preview` and the action changes to an open
eye. The document retains compact anchors:

```text
closed-eye  Table · 4 lines

Alice: Visit [closed-eye] before sundown.

closed-eye  Code block · 5 lines
closed-eye  Divider
```

The inline chip's tooltip names the kind and source, for example
`Ignored autolink: <https://example.com>`.

## Architecture

```mermaid
flowchart LR
    T["IgnoredMarkdown tokens"] --> R["renderDocument:<br/>regions + metadata"]
    R --> P["Scrollable Preview document"]
    P --> C["IgnoredPreviewController"]
    C --> F["Fixed Preview footer:<br/>count + global eye"]
    C --> S["Expanded/collapsed class"]
    S --> B["Block summaries"]
    S --> I["Inline eye chips"]
    L["localStorage preference"] <--> C
```

## Interfaces and responsibilities

| Type | Responsibility |
| --- | --- |
| `renderDocument` | Emits ignored regions with kind, source-line count, and source tooltip metadata. |
| `IgnoredPreviewController` | Owns global state, persistence, footer rendering, and reapplication after Preview renders. |
| `source-view.ts` | Hosts a Preview shell: scrollable document above, fixed footer below; refreshes the controller after each render. |
| `styles.css` | Mirrors the `#END` footer dimensions and defines expanded blocks, collapsed summaries, and inline chips. |

## Key design decisions

### D1 — A footer, not a top toolbar

The Source editor already owns a fixed 37.8-pixel `#END` footer. A same-height Preview footer makes
the split read as two coordinated compiler views. A top toolbar created asymmetric chrome, while
a floating glyph had ambiguous scope.

The footer is always present. At zero it says `0 ignored` and disables the eye, preserving pane
alignment and making "nothing omitted" an explicit status.

### D2 — Collapse Preview only

Source remains the editable truth. Its ignored text stays visible and dimmed so a writer can
locate, select, and change it. The toggle only manages rendered Preview regions.

### D3 — Preserve location and kind

Fully removing ignored content would make the remaining Preview close up around an invisible
omission. Blocks therefore retain one line naming their Marked kind and exact source-line count.
The count is mechanical—not a guessed row count—and remains meaningful for any ignored kind.

Inline content cannot become a row without breaking its sentence. It becomes one closed-eye chip
in place, with a tooltip containing the kind and exact source text.

### D4 — One persisted view preference

Expanded/collapsed is how the writer wants to view the served project, not a property of one
script. One guarded local-storage key applies across file switches and survives hot reloads on the
same report origin. A fresh origin defaults to expanded.

### D5 — Metadata comes from the rendered compiler spans

`renderDocument` already matches `IgnoredMarkdown` spans to Marked tokens. It adds kind and line
metadata at that point, where both the compiler decision and Markdown token are known. The
controller never classifies source or hardcodes which kinds are ignored.

## Boundary cases

| Case | Behavior |
| --- | --- |
| No ignored regions | Footer remains; `0 ignored`; eye disabled. |
| Ignored content changes after save | Count and summaries refresh; current state remains. |
| File switch | New document receives the persisted view state. |
| Ignored inline among words | One inline chip preserves surrounding spaces and flow. |
| Several adjacent inline regions | One chip per compiler-projected span. |
| Preview pane hidden | Footer hides with its Preview shell. |
| Narrow stacked layout | Footer stays below the Preview document, matching desktop semantics. |
| Storage unavailable | Toggle still works for the current view; it simply does not persist. |

## Testability

- **Renderer unit tests:** metadata for each supported Marked kind, exact source-line counts, and
  escaped inline tooltip source.
- **Controller unit tests:** zero/expanded/collapsed footer states, action semantics, persistence,
  refresh after rerender, and storage failure.
- **Source-view integration:** fixed footer always exists, global state applies to new regions,
  Source remains unchanged, and Preview hide/show owns the whole shell.
- **Live integration:** a real `dialogue.toml` sets `autolink = "ignore"`; Preview collapses its
  rendered link to an inline eye chip and restores it globally.
