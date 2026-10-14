# Language surveys

Dated reviews of the script language as a whole: what it lacks next to comparable
dialogue languages, and how well each existing construct works. A survey guides
which construct to build, emphasize, extend or remove next. It is evidence for a
design note, not a substitute for one: a construct is still designed and approved
through its own note under [language](../design-notes/README.md#language-constructs).

## How surveys work

- **Each survey is a snapshot.** Its file name starts with the survey date, and its
  opening note names the commit it was taken against. It is not edited to track the
  language afterward, so its numbers stay comparable.
- **A new survey builds on the last one.** Start from the most recent survey of the
  same kind. Re-score against the current `main`, and add a section on what changed
  since: features built, findings fixed, verdicts revised. Keep the same metrics and
  scales unless the new survey says why they changed.
- **Corrections are dated.** A factual error found later gets a short dated
  correction note in the survey, not a silent rewrite.

## Surveys

| Date | Survey | Baseline | What it covers |
| --- | --- | --- | --- |
| 2026-10-14 | [Language feature gaps](2026-10-14%20Language%20Feature%20Gaps.md) | `8d25661` | Six missing features ranked against Ink, Yarn Spinner, Godot Dialogue Manager, ChoiceScript and Ren'Py, with proposed syntax and a fresh-reader check. |
| 2026-10-14 | [Construct audit](2026-10-14%20Construct%20Audit.md) | `8d25661` | Every existing construct scored for usefulness, intuitiveness, Markdown fit and safety, with a verdict per construct. |
