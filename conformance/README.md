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
  source.dialogue.md   the compile, opening with a broken: comment that shows the edit
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

`baseline/` is accepted and nothing is wrong with it. **A refusal is an accepted
document with exactly one field changed.** The line-level cases all share
`baseline/`, so a diff between one of them and it is the single line that case is
about; a case built on a richer construct ships its own accepted source beside its
playbook, and its `because` names the edit. Either way, because the accepted
document passes, a refusal can only be caused by the field its case touched, which
pins the *reason* for each refusal without asserting a word of any message.

### The schema is not enough

Eleven of the seventeen refusals under `readable/` are **valid by the JSON
Schema**. A schema describes shape: it can say `entry` is a non-negative integer,
but not that there are only two nodes to point at; it can say `version` is an
integer, but not which versions a build reads.

| Refusal | Caught by |
| --- | --- |
| A target written as text; a truncated file; a foreign arm kind; two successions on one node; a lone else; a second else | the schema |
| A version too new, an unknown capability, a node out of position, all four kinds of dangling reference, a node with no way out, and branch arms out of order, sharing an order, or led by the else | only a reader |

So validating against the schema is necessary but not sufficient, and that gap is
a large part of why this corpus exists.

## The playable half

`playable/` arrives with the runtime ([C2]). A fixture there is one **session**:
the messages a driver sends, interleaved with the replies a runner must give.

```json
{ "send": "next" },
{ "expect": { "said": { "speaker": "Alice", "speech": "Hello." } } }
```

A runtime is conformant when it can hold every session in the corpus — not merely
produce the same story, but have the same conversation.

To run one, walk the `session` in order:

1. `send` — deliver that message to the runner.
2. `expect` — take the runtime's **next** message and compare. Never search ahead
   for a match: a harness that did would accept a runner that reordered its
   replies, which is most of what a session is for.
3. At the end, the run must be finished too. A run that stops early and a session
   that stops early are different failures, and saying which is which is what
   tells an author whether the fixture or the runtime is wrong.

Comparison is ordinary equality, with the two exceptions above: `speech` and
`label` compare as fragments when written as an array and as the flattening when
written as a string, and an absent `speaker` means the anonymous default speaker
rather than "any speaker".

A session may send a command the run cannot take, and assert the refusal:

```json
{ "send": "next" },
{ "expect": { "refused": { "reason": "already-ended" } } }
```

`reason` is one of the protocol's closed set. The prose a refusal also carries is
written for a contributor, so the corpus asserts the reason rather than the words —
the same rule the readable half applies to a reader's message.

## Adding a case

1. Write `source.dialogue.md`, as small as it can be while showing the one thing
   the case is about.
2. Compile it: `ddown compile <source> --emit playbook -o playbook.json`.
3. For a refusal, change **one** field of that playbook by hand — a compiler will
   not emit a broken document, so the edit is the only way to write the case.
4. For a refusal, open `source.dialogue.md` with a **`broken:` block**: an HTML
   comment naming the edit and showing it — the invalid script where the language
   can express it, otherwise the changed part of the playbook. The comment is
   ignored, and the script below it still compiles.
5. Write `fixture.json`, and say in `because` what a reviewer should weigh.

Keep a case minimal and about one thing: a failure should name the construct, not
send someone reading a script.

## Where the design lives

- [Conformance corpus](../docs/contributing/design-notes/runtime/Conformance%20Corpus.md)
  — the format and the decisions behind it.
- [`schema/fixture-0.schema.json`](../schema/fixture-0.schema.json) — the fixture
  format itself. Every fixture carries a `$schema` pointing at it, so an editor
  checks a case while you write it, and CI checks every case in the corpus.
- [Playbook format](../docs/contributing/design-notes/runtime/Playbook%20Format.md) — the
  document these fixtures are about.
- [`schema/playbook-0.schema.json`](../schema/playbook-0.schema.json) — the
  format's schema. Every case the corpus **accepts** validates against it.

[C2]: https://github.com/pengzhengyi/dialoguedown/issues/297
