---
applyTo: "src/DialogueDown.Visualization/web/**"
---

# Visualization frontend conventions

The compilation report's client is a self-contained **TypeScript + Vite** project
in `src/DialogueDown.Visualization/web/`. The .NET library embeds its **built**
single-file report (`web/dist/report.html`), which is committed, so a plain
`dotnet build` needs no Node. You only need **Node 24+** to change the client.
Full context is in [`CONTRIBUTING.md`](../../CONTRIBUTING.md).

## Workflow

Run from `src/DialogueDown.Visualization/web/`:

```bash
npm install
npm run dev      # live-reloading dev server with sample data
npm run check    # typecheck + eslint + stylelint + prettier + vitest
npm run build    # rebuild the committed dist/report.html
npm run e2e      # Playwright end-to-end + accessibility (needs: npx playwright install chromium)
npm run e2e:live # build the CLI once, then run the real loopback-server E2E suite
```

For inner-loop feedback, use the VS Code tasks `web: test file`,
`web: test watch`, `web: e2e file`, `web: e2e grep`, or
`web: e2e live file`. These narrow the test scope; they never replace
`web: check` and the full static/live suites before pushing.

## Rules

- **Run `npm run check` before committing.** It must pass — it is the same gate CI
  runs.
- **Rebuild and commit `web/dist/report.html`** whenever you change anything under
  `web/src`, **or a file it embeds** — the report imports
  `schema/playbook-0.schema.json` (`src/playbook-schema.ts`), so a schema change
  moves the bundle too. The **Sync report bundle** workflow rebuilds it for
  forgotten changes, but committing it yourself keeps CI green on the first run.
- Let the tooling format and lint: follow `eslint.config.js`, `.stylelintrc.json`,
  and `.prettierrc.json` rather than hand-formatting or overriding rules inline.
- Frontend quality tools keep content-aware incremental data under ignored
  `web/.cache/`; the repository `clean` task removes it for a cold run.
- Write tests for behavior with **Vitest** (unit) and **Playwright** (end-to-end);
  keep the report **self-contained** and offline-capable — no external CDNs.
- **Preview UI changes before committing.** Open the dev server (`npm run dev`) or
  a built report (`npm run build`, then open `dist/report.html`) and interact with
  the change to confirm it looks and behaves right.
- Live end-to-end tests run with `npm run e2e:live`. The command builds the CLI
  once; each Playwright server launches that Release DLL directly. Do not replace
  the shared launcher with per-server `dotnet run` calls.

## Comments

The shared rules — true now, standing alone, plain words, the fact rather than the
argument, an example — are in
[`copilot-instructions.md`](../copilot-instructions.md#comments). In the client:

- A module opens with a one- or two-sentence comment saying what it does.
- **TSDoc** (`/** */`) documents exported functions, types, and module state whose
  purpose is not obvious. The first sentence says what it is; `{@link name}` refers
  to an identifier.
- A CSS rule gets a comment only for a layout reason the selector does not show,
  stated as it holds now.
- **End-to-end tests** describe the condition a test guards ("in a 1280×640
  window, a tall selection keeps the footer on screen"), not the bug it once was
  ("used to push the footer off"). A viewport size, a wait, or a retry gets a
  comment when its reason is not visible.
