# README Shipping-Status Refresh

> [!NOTE]
> Status: **implemented**. A documentation-only pass that made the README's
> visualization section match what actually ships, with no code change.

## Goal & scope

The README's visualization prose has drifted behind the code. It claims only the
Markdown AST view ships and that the report surfaces "Mermaid and DOT text for
quick embedding" — but the report now shows **four tabs** (Source, Markdown AST,
Dialogue AST, Desugared AST), runs a **View ⇄ Edit** editor, and does **not**
surface Mermaid/DOT in its UI. This component reconciles the README with reality.

In scope — the README's visualization paragraph and its immediate claims:

- Correct the shipped stages: Source + Markdown AST + Dialogue AST + Desugared AST.
- Drop the "Mermaid and DOT text for quick embedding" claim from the report
  description (the interactive report does not expose them today; a Rendering Mode
  and a CLI DOT emit are separate, later components).
- Refresh the bundled-libraries list (CodeMirror is now part of the report).
- Mention the View ⇄ Edit editor briefly, matching the CLI examples already shown.

Out of scope:

- Any code, CLI, or renderer change.
- The Mermaid rendering mode and the DOT CLI emit — their own components.
- Restructuring the README beyond the visualization section.

## Functionality checklist

- [x] The "Markdown AST ships / Dialogue AST added later" sentence is replaced with
      the four shipping tabs.
- [x] The "Mermaid and DOT text for quick embedding" claim is removed from the
      report description (no stale capability claim).
- [x] The bundled-libraries list reflects the current bundle (adds CodeMirror).
- [x] A brief View ⇄ Edit mention aligns the prose with the CLI examples.
- [x] `markdownlint` shows no new issues; the doc reads cleanly.

## Key design decisions

### D1 — Describe only what the report actually does

The README is user-facing, so it must not advertise a capability the report does
not have. The report renders Mermaid only when an author writes a fenced
`mermaid` block in the dialogue Markdown; it does not offer Mermaid versions of
compiler-stage graphs. Graphviz DOT remains a separate CLI emission mode.

### D2 — Keep it a light touch

This is a targeted correction of stale claims, not a rewrite. Preserve the
section's voice, structure, and surrounding content; change only the inaccurate
sentences and the bundle list.

## Integration

Pure documentation. No effect on code, tests, CI (markdownlint is not a CI gate),
or the committed report bundle. Sibling notes already read correctly, so no
cross-doc reconciliation is required beyond the README.

## Testability

- **Manual read-through** against the shipped report (four tabs, View/Edit, the
  bundle contents).
- **`markdownlint`** on the README to catch new formatting issues (long-line
  MD013 is pre-existing and accepted, matching the rest of the docs).
