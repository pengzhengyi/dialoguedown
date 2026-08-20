# Contributing to DialogueDown

Thank you for considering a contribution. DialogueDown is early stage, so
small, well-scoped changes are easiest to review and merge.

## Ways to contribute

- Report bugs with a minimal reproduction.
- Propose script-language or API improvements.
- Improve documentation and examples.
- Add tests for current behavior.
- Fix small, focused issues.

## Before you start

1. Search existing issues and pull requests to avoid duplicate work.
2. Open an issue for larger behavior, API, or syntax changes before writing code.
3. Keep pull requests small and focused.

## Development setup

Requirements:

- .NET SDK 8 or newer
- Git

Clone the repository and run:

```bash
dotnet restore DialogueDown.sln
dotnet format DialogueDown.sln --verify-no-changes --no-restore
dotnet build DialogueDown.sln --configuration Release --no-restore
dotnet test DialogueDown.sln --configuration Release --no-build --minimum-expected-tests 3000
```

The `format` step is the code-style gate, and CI runs it first. Run it locally
too: code-style rules such as naming (`IDE####`) do not run during
`dotnet build`, so a green build alone does not mean a green CI. Analyzer
warnings also appear only when a project actually recompiles — repeating
`dotnet build` with no changes prints zero warnings even when violations exist.
Add `--no-incremental` when a build's warning count has to be trusted.

To collect coverage focused on production source code:

```bash
dotnet tool restore
dotnet test DialogueDown.sln \
  --coverlet \
  --coverlet-output-format cobertura \
  --coverlet-include "[DialogueDown*]*" \
  --minimum-expected-tests 3000
dotnet reportgenerator \
  "-reports:TestResults/coverage.cobertura.*.xml" \
  "-targetdir:coverage-report" \
  "-reporttypes:Html;MarkdownSummary;Cobertura"
```

The include filter is what keeps the number meaningful: the runner instruments
every assembly it loads, so without it a dependency's untested code counts
against the project and the figure drops to a quarter of the real one. Cobertura
output is written under `TestResults/`, and the interactive report to
`coverage-report/index.html`.

CI fails below 90% line coverage and warns below 100%.

### Core quality guardrails

The core library (`src/DialogueDown`) holds itself to size, complexity, and
shape limits that **fail the build** on a regression (they are analyzer errors,
not warnings). The CLI and visualization projects are intentionally exempt.

- **Size and complexity** — SonarAnalyzer caps method length (≤ 40 lines), file
  length (≤ 400 lines), parameters (≤ 7), and cyclomatic (≤ 10) / cognitive
  (≤ 15) complexity. Thresholds live in `src/DialogueDown/SonarLint.xml`;
  severities and scope in `.editorconfig`.
- **No mutable global state** — `CA2211` forbids externally visible non-constant
  static fields.
- **No God classes** — an architecture test caps public methods per core type
  (≤ 20); private helpers are not counted, so decomposing into small methods is
  encouraged.
- **Named namespaces** — an architecture test caps an assembly's root namespace
  at 10 types, so a layer cannot flatten into an unnamed list.
- **A reproducible compile** — `RS0030` forbids the core from reading the clock,
  minting a `Guid`, or drawing randomness, so the same script always lowers to
  the same graph. `src/DialogueDown/BannedSymbols.txt` names each banned API and
  why. (Random *choice* is unaffected — the player resolves it at runtime.)
- **An immutable Dialogue AST** — an architecture test holds every AST node
  immutable, so no later stage can change what an earlier one produced.

Use the `build: fast` task (analyzers off) for the inner loop, but run the
normal analyzer-enabled `build`/`test` before pushing.

### Adding or updating a NuGet package

Package **versions are managed centrally**: every version for the whole solution
is declared once in [`Directory.Packages.props`](Directory.Packages.props), and a
project references a package by name only.

```xml
<!-- Directory.Packages.props — the version, once -->
<PackageVersion Include="Markdig" Version="1.3.2" />

<!-- any .csproj — the reference, no version -->
<PackageReference Include="Markdig" />
```

A version in a `.csproj` is an error under central management, which is the
point: two projects cannot drift onto different versions of the same package.
Dependabot updates `Directory.Packages.props` directly, so a bump lands in one
place for every project that uses it.

A project that genuinely needs a different version says so out loud, with
`VersionOverride` on its own `PackageReference`. Divergence stays possible; it
just stops being something that can happen by accident.

> [!IMPORTANT]
> Do not enable `CentralPackageTransitivePinningEnabled`. It promotes pinned
> transitive dependencies into the generated `.nuspec`, so the published
> `DialogueDown` and `DialogueDown.Cli` packages would declare dependencies
> nobody chose. A test guards this.

### Visualization frontend (`web/`)

The compilation report's client is a self-contained TypeScript + Vite project in
`src/DialogueDown.Visualization/web/`. The .NET library embeds its **built**
single-file report (`web/dist/report.html`), which is committed to the repo, so a
plain `dotnet build` needs no Node. You only need Node (24+) to change the client:

```bash
cd src/DialogueDown.Visualization/web
npm install
npm run dev                        # live-reloading dev server with sample data
npm run check                      # typecheck, lint, style, format, unit tests
npx playwright install chromium    # once, for e2e
npm run e2e                        # Playwright end-to-end + accessibility tests
npm run build                      # rebuild the committed dist/report.html
```

If you change anything under `web/src`, rebuild and commit `web/dist/report.html`
so it stays in sync with its sources. As a safety net the **Sync report bundle**
workflow rebuilds and commits it for you on pull requests that forget to (including
Dependabot build-tool bumps), but committing it yourself keeps CI green on the first
run instead of after an automatic follow-up commit.

### The `visualize` CLI and live server

`src/DialogueDown.Visualization.Live` is a small console app (and loopback server)
for viewing a script's compilation:

```bash
cd src/DialogueDown.Visualization.Live
dotnet run -- path/to/scene.dialogue.md            # render + open a static report
dotnet run -- path/to/scene.dialogue.md --watch    # serve + hot-reload on file changes
dotnet run -- path/to/scene.dialogue.md -o out.html --no-open   # write, don't open
```

Watch mode starts a `127.0.0.1`-only server that pushes recompiled stages to the
browser over Server-Sent Events; it is a development tool, not a hosted service.
The live end-to-end tests run with `npm run e2e:live` in `web/`. The command
builds the CLI once, then launches each loopback server from that Release DLL
without repeating project builds.

### Editor tasks (VS Code)

Common tasks are wired up in `.vscode/tasks.json` (**Terminal → Run Task**), so
you can build, test, and clean without memorising commands: `build` / `test`
(.NET), `build: fast` (inner-loop compile without analyzers), `test: project` /
`test: filter` / `test: class` (one already-built .NET test scope), `web: build` / `web: check` /
`web: e2e` (frontend), targeted `web: test file` / `web: test watch` /
`web: e2e file` / `web: e2e grep` / `web: e2e live file`, `build: all` and
`verify: all` (both stacks), and `clean` (remove build/test artifacts). Always
run the normal analyzer-enabled build/test and full frontend gates before
pushing.

## Commit style

Use [Conventional Commits](https://www.conventionalcommits.org/):

```text
docs: improve script language examples
fix(parser): handle empty dialogue files
test(tags): cover default speaker tag
```

Use one logical change per commit. Mark breaking API or script-language changes
with `BREAKING CHANGE:` in the commit footer.

## Pull request checklist

Before opening a pull request:

- [ ] Add or update tests for behavior changes.
- [ ] Update documentation for public API or script-language changes.
- [ ] Run `dotnet format DialogueDown.sln --verify-no-changes` — the same code-style gate CI runs.
- [ ] Run `dotnet test DialogueDown.sln --configuration Release --no-build --minimum-expected-tests 3000`.
- [ ] Run source-focused coverage when changing tested behavior.
- [ ] If you changed the visualization frontend (`web/`), rebuild and commit `web/dist/report.html` (CI auto-commits it if you forget).
- [ ] Keep the pull request focused on one topic.
- [ ] Explain why the change is useful.

## Code of conduct

All participants must follow the [Code of Conduct](CODE_OF_CONDUCT.md).
