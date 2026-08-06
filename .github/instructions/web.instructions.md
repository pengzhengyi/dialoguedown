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
  `web/src`, so the committed bundle stays in sync with its sources. The
  **Sync report bundle** workflow rebuilds it for forgotten changes, but committing
  it yourself keeps CI green on the first run.
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
