# Interactive Playthrough

> [!NOTE]
> Status: **explored — not adopted**. Three prototypes played a hand-written scenario as a text
> adventure to check branching by feel; none is merged. The compiled
> [playbook](../runtime/Playbook%20Format.md) and the [runtime](../runtime/Runner.md) are
> the foundation any adopted player would use.

## Goal

Answer *"does this dialogue actually play?"* by reading a line, picking a choice, following the
jump, and reaching an ending — validation by playing rather than by reading a graph. The report's
[Playbook Tab](../visualization/report/Playbook%20Tab.md) shows the compiled structure; a
playthrough would walk it.

## Directions tried

All three read one small hand-written model (scenes of speeches ending in choices or an ending), so
each played identical branching.

| Direction | How | New dependency | Validates |
| --- | --- | --- | --- |
| **A. Terminal player** | A `ddown play` command using Spectre.Console's `SelectionPrompt` and markup | None | The real graph |
| **B. Web Play tab** | A report tab with a transcript and choice buttons, rendering emphasis with `marked` | None | The real graph |
| **C. Web Yarn tab** | Export to classic Yarn text, run it with `yarn-bound` (wrapping `bondage.js`) | `yarn-bound` (ISC), ~48 KB raw / 24 KB gzip | The graph **and** the exporter |

## Findings worth keeping

- **Render our own graph for validation.** A or B exercises the compiled dialogue with no
  translation layer. An interactive-fiction engine (C) validates the exporter as much as the script,
  so it suits a shippable-runtime goal, not a checking tool.
- **Spectre.Console covers the terminal.** `SelectionPrompt<T>` gives arrow-key choices; the player
  lives in the CLI, outside the engine-agnostic core. It must check
  `console.Profile.Capabilities.Interactive`, because a prompt throws without a TTY, and escape text
  before converting emphasis to Spectre markup.
- **A gated dynamic import removes an optional runtime.** A top-level import of a side-effectful UMD
  module (`yarn-bound`) stayed in the production bundle even when its tab was dead code.
- **Yarn syntax that `bondage.js` accepts.** Classic nodes (`title:` / `---` / `===`), `->` options
  with an indented `<<jump node>>`; node names cannot contain hyphens, so map `-` to `_`.

## If adopted

A player should read the compiled playbook and drive the runtime's stepping rather than a
hand-written model, and take a script argument (`ddown play <script>`). A Yarn export, if wanted,
is its own exporter reading the playbook, with its own tests.
