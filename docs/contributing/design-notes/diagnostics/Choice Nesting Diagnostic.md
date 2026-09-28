# Choice Nesting Diagnostic

> [!NOTE]
> Status: **implemented**. `DLG3002` warns when a branch reaches a fourth level of
> nested choices, suggesting a new scene and a jump instead. Deep nesting stays
> valid; a configurable threshold, suppression, and an automatic fix are not built.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Choice group** | One `Choices` or `RandomChoices` block: the options offered together. |
| **Choice nesting level** | The number of choice groups on the path from the document body to a group, counting that group. |
| **Recommended maximum** | The deepest level that reports nothing: 3. |
| **First over-limit group** | A group at level 4 whose nearest enclosing group is within the maximum. |
| **Choice branch** | One path through nested groups; sibling paths are separate branches. |

## Writer-facing behavior

```markdown
- Level 1
    - Level 2
        - Level 3
            - Level 4      ← DLG3002 here, once
                - Level 5  ← no second report on this branch
```

| Field | Value |
| --- | --- |
| Code | `DLG3002` |
| Title | `Deeply nested choice branch` |
| Category / severity | `Style` / `Warning` |
| Message | `This branch reaches choice nesting level {0}; the recommended maximum is {1}. Consider moving this branch into a new scene and jumping to it instead.` |

`DLG3001` is unused.

## Design

`ChoiceNestingDepthRule` is a
[structural validator](Diagnostics%20and%20Validation.md#the-structural-validator)
rule. `DialogueTreeIndex` records parent links during its single traversal and
yields a node's ancestors nearest first; the rule counts the choice-group ancestors
of each `Choices` and `RandomChoices` — both add indentation depth — and reports
when that count equals the maximum.

## Key design decisions

### D1 — A style warning

The script is valid and its meaning is unambiguous; the concern is readability, so
the code is in the `DLG3xxx` range. `Warning`, not `Info`, because the advice is
actionable, matching the other advisories such as `DLG1003`.

### D2 — The maximum is 3, behind an internal seam

A top-level group is level 1; two nested follow-ups stay readable, and a fourth
indentation level is where extracting a scene helps. The rule takes the maximum
through an internal constructor (positive, validated) so a future rule-configuration
design can expose it without growing `CompilerOptions` for one rule.

### D3 — Report once per branch, at the first violation

Deeper descendants repeat the same structural choice, so they stay silent;
independent siblings that each reach level 4 each report. This follows PMD's
`AvoidDeeplyNestedIfStmts` and Sonar S134 rather than ESLint's `max-depth`, which
reports every deeper level. The surveyed interactive-fiction tools (Ink, Yarn
Spinner, ChoiceScript, Ren'Py) all keep nesting legal with no depth diagnostic; Ink's
guide recommends diverting to a new stitch, which is the advice this message gives.

### D4 — Point at the group's start

A group's span can cover many lines. The diagnostic uses an empty span at
`group.Span.Start`, so renderers mark the first marker instead of underlining the
whole block. The Dialogue AST does not keep the bullet's own token span.

### D5 — Parent links use reference identity

Nodes are records, so structurally equal nodes compare equal; the index keys parents
by reference. The ancestry query is reusable by other structural rules.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| No choices, or one wide group | No diagnostic; breadth is not nesting. |
| Levels 1–3 | No diagnostic. |
| Level 4 | One report at the group's start. |
| Level 5+ below a reported group | No further report. |
| Two branches reach level 4 | One report each. |
| Ordered, unordered, or random groups | Counted alike. |
| Non-choice nodes between groups | Do not count. |
| Non-positive maximum | `ArgumentOutOfRangeException`. |

## Testability

- Rule tests: the exact boundary, first violation, no descendant duplicate, sibling
  violations, ordered choices, custom thresholds.
- Index tests: parent and ancestor links use reference identity.
- A compiler test: a four-level script yields one located `DLG3002` and still
  succeeds; the error-code reference's examples are compiled.
