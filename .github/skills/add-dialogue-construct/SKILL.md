---
name: add-dialogue-construct
description: Design and add one DialogueDown language construct through comparative research, docs-first approval, procedural core TDD, headless visualization adaptation, preview review, and the OSS pull-request lifecycle.
---

# Add a DialogueDown language construct

Use this skill for one coherent syntax or semantic construct at a time. Invoke
and compose `git-pr-flow`, `maintain-oss`, and `design-review-implement` across
the full construct loop: `git-pr-flow` owns the branch, the commits, and the
pull request; `maintain-oss` owns the tracked issue and the changelog. Invoke
`polish-tech-doc` whenever a design note, specification, or related
documentation is created or revised.

Those skills own their normal approval and execution gates. This skill adds the
DialogueDown-specific language-design questions and required project surfaces;
it does not replace or bypass the delegated workflows.

## Working rules

- Discuss the language shape before writing implementation code.
- Use one domain term consistently across research, docs, code, tests,
  diagnostics, commits, and changelog.
- Treat the language specification and examples as part of the public interface.
- Do not implement until the user explicitly approves the design note and
  writer-facing specification.
- Develop with `git-pr-flow`'s **branch-on-main model** — a feature branch in
  the primary checkout, not a separate worktree. A language construct runs
  through the whole pipeline and its visualization, so it wants the editor,
  debugger, and live server the user already has pointed at the repository. If
  unrelated work blocks a safe branch switch, stop and ask.
- Confirm the mode for every construct. Default to **procedural** core work and
  **headless** visualization/finalization work.
- Follow the current repository instructions and `CONTRIBUTING.md` for commands,
  tools, checks, generated artifacts, and branch conventions. Do not duplicate
  volatile command details here.
- Do not merge until the user reviews the preview and explicitly says **"Skill
  Complete"**.

## 1. Research and agree on the shape

First define the writer problem, where the construct may appear, its observable
meaning, its boundaries, and which compiler stages or tools it may affect.

Study comparable implementations through official documentation, source, or
parser tests. Always include:

- Yarn Spinner;
- Ink;
- at least one closer analog, such as Twine, ChoiceScript, Ren'Py, CommonMark,
  MDX, Liquid, Python, C#, or another established language.

Compare the candidates by:

| Concern | What to learn |
| --- | --- |
| Syntax | What authors type and where it is legal |
| Semantics | What the parser, compiler, or runtime produces |
| Scope and precedence | How it nests and interacts with neighboring syntax |
| Literal text | How authors express the same punctuation without invoking it |
| Errors | How incomplete, malformed, and ambiguous forms recover |
| Tooling | How previews, highlighting, completion, and diagnostics represent it |
| Experience | What is readable, surprising, or error-prone in practice |

Evaluate every proposed shape from three perspectives:

1. **Writer usability** — ease of scanning, typing burden, memorability, repeated
   or nested use, and clarity for non-technical writers.
2. **Markdown alignment** — how CommonMark/Markdig parses it, what an ordinary
   Markdown preview shows, and whether it collides with existing Markdown.
3. **Language safety** — ambiguity, whitespace and Unicode boundaries, nesting,
   missing delimiters, error recovery, future delimiter pressure, and any
   literal character sequence the new syntax would consume.

Every consumed literal sequence needs a clear escape or an explicit, approved
loss of expressibility. For a novel or ambiguous form, show raw examples to a
fresh reader and treat plausible misreadings as design evidence.

Present two to four viable shapes with tradeoffs and a recommendation. Ask one
focused decision question at a time. Continue only after the user agrees on the
scope, syntax, semantics, and literal-text strategy.

## 2. Design and reconcile the docs first

Draft a proposed design note in the repository's design-note collection
(currently `docs/contributing/design-notes/`). Record the lasting lessons from
comparable languages without turning the note into a history of rejected
options.

Cover the construct's:

- goal and bounded scope;
- writer-facing behavior;
- grammar and semantics;
- affected compiler stages and integration seams;
- Markdown interaction;
- ambiguity, escaping, diagnostics, recovery, and source spans;
- test strategy;
- open questions and deferred work.

Reconcile the writer-facing language specification (currently
`docs/guide/script-language.md`):

- add the construct to the navigation and syntax summary;
- add its focused reference section;
- document canonical use, placement, whitespace, nesting, literal punctuation,
  and preview behavior;
- update nearby claims that the construct changes;
- incorporate it naturally into the complete example.

Update the gallery dialogue (currently `examples/gallery.dialogue.md`) with a
natural use of the construct, so the static website demonstrates it. Keep the
example a coherent dialogue, not a syntax catalog. Register the design note in
the current reading guide and documentation navigation.

Run `polish-tech-doc` in internal developer-facing mode for the design note and
user-facing mode for the specification and gallery. Use fresh-reader testing
when the new syntax is substantial or easy to misunderstand.

Present the research conclusion and documentation diffs. Iterate until the user
explicitly approves the design and writer-facing contract. Do not implement
before this gate.

## 3. Start the approved branch

After documentation approval:

1. At the single mode gate, confirm **procedural core** and **headless
   visualization/finalization**, or record the user's alternatives.
2. From an up-to-date `main`, create a construct feature branch in the primary
   checkout, carrying only the approved document drafts. `git-pr-flow` covers
   refreshing `main` first and naming the branch.
3. Preserve reviewable history: commit the approved design note separately from
   the specification and gallery. Track the work as an issue per `maintain-oss`.

Obtain the commit approvals required by the selected mode and `git-pr-flow`.
Never commit the drafts directly to `main`.

## 4. Implement the core

Follow the construct through the relevant part of the compiler pipeline:

```text
source -> Markdown AST -> Dialogue AST -> desugared AST
       -> semantic model -> graph/runtime
```

In the default procedural mode, repeat one small TDD increment at a time:

1. Write a focused failing test.
2. Implement the minimum behavior.
3. Refactor with tests green.
4. Run the smallest relevant quality gates.
5. Present the unstaged diff and results.
6. Wait for approval before committing or starting the next increment.

Cover the boundaries relevant to the construct: canonical forms, Markdown
context, whitespace and Unicode, precedence and nesting, literal punctuation,
malformed input, diagnostics, source spans, AST/lowering semantics, conflicts,
and public API or architecture impact.

If implementation reveals a design flaw, stop at the next review gate. Explain
the tradeoff, update and re-polish the note and specification, and obtain
approval before continuing.

Proceed only after the core is fully validated and approved under the selected
mode.

## 5. Adapt visualization and finalize

Under the default headless mode, adapt the visualization autonomously through
small semantic commits. Touch only the surfaces the construct affects, such as
compiler projections, report data, source-editor assistance, rendering, help,
or diagnostics.

**Extend the compiler-projected editor surfaces for the new construct.** Syntax
highlighting and autocompletion are projected from the compiler, not re-derived in
the browser (see the *Compiler-Projected Editor Semantics* design note). So a
construct that introduces new syntax or new completable names must be taught to
those projections, or it will show up unhighlighted and uncompletable:

- **Highlighting** — if the construct adds a distinct token (a new sigil, keyword,
  or delimiter), add a `TokenKind` and emit it from the semantic-token projection,
  then give it a themed color in the editor styles.
- **Completion** — if the construct introduces names a writer would complete (a new
  kind of speaker, tag, label, or reference), surface them through the semantic
  symbol projection so completion offers them.

Keep the test pyramid: prefer pure and component tests, then add only the static
and live browser coverage needed to prove real integration. Rebuild committed
generated artifacts when required by current repository instructions.

Ensure the gallery dialogue exercises the construct and the static website
renders it meaningfully.

Crosscheck the result against the approved design:

- **Achieved** — implemented and tested as designed;
- **Changed** — rewrite the docs in place to match the final design;
- **Not implemented** — mark it deferred and link a tracked issue.

Set the design note to implemented, reconcile terminology across docs and code,
and add one concise domain-level `Unreleased` changelog entry. Polish the final
docs and run every current repository gate required by the changed surfaces.

Build and open both the served visualization and static gallery demo using the
repository's current documented workflow. Prompt the user to review source
readability, Markdown behavior, compiler-stage representations, editor support,
diagnostics, and the gallery example.

Treat edits made during preview as review feedback: validate them, reconcile the
docs, commit them under the selected mode, and preview again. Do not publish the
branch until every intended change is committed, and the working tree is clean.

## 6. Complete the OSS loop

Push the branch and open a pull request through the repository template, then
follow `git-pr-flow` through keeping the branch current, merging, resyncing
local `main`, and deleting the merged branch. Link the tracked issue, and
summarize the language decision and the writer tradeoffs behind it.

This skill adds one gate on top of that flow: **do not merge until the user has
approved the preview and explicitly says "Skill Complete"** — the preview is
where a construct's writer-facing shape is judged, and no amount of green CI
substitutes for it. Confirm the primary checkout is clean afterwards.
