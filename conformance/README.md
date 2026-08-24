# Conformance corpus

Language-neutral fixtures that keep every DialogueDown runtime telling the same
story. They are plain data: a runtime in any language can run them without
building anything in this repository.

The fixtures are **hand-authored from the design**, not recorded from a run. A
corpus recorded from an implementation can only prove that implementation agrees
with itself; one written by hand is a specification the implementation must meet.

## Layout

```text
conformance/
  readable/     can a reader load this document at all
  playable/     does a runner hold the same conversation   (arrives with C2)
```

Each case is a folder, and `fixture.json` is always the entry point:

```text
readable/entry-leads-nowhere/
  fixture.json         what a reader must do with the document, and why
  playbook.json        the document itself
  source.dialogue.md   what it was compiled from, so a reviewer reads dialogue
```

## The readable half

A `fixture.json` states a verdict:

```json
{
  "name": "entry leads nowhere is refused",
  "playbook": "playbook.json",
  "verdict": "refuse",
  "because": "the entry points past the last node, so there is nowhere to begin"
}
```

To run the corpus, for every folder under `readable/`:

1. read `fixture.json`;
2. load the document it names with your playbook reader;
3. `accept` means the load must succeed; `refuse` means it must fail.

**A refusal's message is not asserted.** Every runtime should explain itself in
its own language, and pinning English here would make the corpus untranslatable.
`because` is for the human reading the file.

### Why one case is `baseline`

`baseline/` is accepted and nothing is wrong with it. **Every refusal is that same
document with exactly one field changed.** So a diff between any case and the
baseline is the single line that case is about — and because the baseline passes,
a refusal can only be caused by the field its case touched. That pins the *reason*
for each refusal without asserting a word of any message.

### The schema is not enough

Seven of the nine refusals under `readable/` are **valid by the JSON Schema**. A
schema describes shape: it can say `entry` is a non-negative integer, but not
that there are only two nodes to point at; it can say `version` is an integer,
but not which versions a build reads.

| Refusal | Caught by |
| --- | --- |
| A target written as text; a truncated file | the schema |
| A version too new, an unknown capability, a node out of position, and all four kinds of dangling reference | only a reader |

So validating against the schema is necessary but not sufficient, and that gap is
a large part of why this corpus exists.

## The playable half

`playable/` arrives with the runtime ([C2]). A fixture there is one **session**:
the messages a driver sends, interleaved with the replies a runner must give.

```json
{ "send": "continue" },
{ "expect": { "said": { "speaker": "Alice", "speech": "Hello." } } }
```

A runtime is conformant when it can hold every session in the corpus — not merely
produce the same story, but have the same conversation.

## Adding a case

1. Write `source.dialogue.md`, as small as it can be while showing the one thing
   the case is about.
2. Compile it: `ddown compile <source> --emit playbook -o playbook.json`.
3. For a refusal, change **one** field of that playbook by hand — a compiler will
   not emit a broken document, so the edit is the only way to write the case.
4. Write `fixture.json`, and say in `because` what a reviewer should weigh.

Keep a case minimal and about one thing: a failure should name the construct, not
send someone reading a script.

## Where the design lives

- [Conformance corpus](../docs/contributing/design-notes/Conformance%20Corpus.md)
  — the format and the decisions behind it.
- [Playbook format](../docs/contributing/design-notes/Playbook%20Format.md) — the
  document these fixtures are about.
- [`schema/playbook-0.schema.json`](../schema/playbook-0.schema.json) — the
  format's schema. Every case the corpus **accepts** validates against it.

[C2]: https://github.com/pengzhengyi/dialoguedown/issues/297
