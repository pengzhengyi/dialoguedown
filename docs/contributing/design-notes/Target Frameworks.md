# Target Frameworks

> [!NOTE]
> Status: **implemented**. The shipped libraries multi-target `net8.0` and `net10.0`; the CLI and
> the diagnostics-only projects target `net10.0`.

## Table of contents

- [Goal](#goal)
- [The problem: two clocks, one deadline](#the-problem-two-clocks-one-deadline)
- [The shape](#the-shape)
- [Key design decisions](#key-design-decisions)
- [Boundary cases](#boundary-cases)
- [Testability](#testability)
- [When Godot moves](#when-godot-moves)

## Goal

Keep DialogueDown on a supported .NET runtime without dictating when a game using it must move.

## The problem: two clocks, one deadline

**.NET 8 and .NET 9 both reach end of support on November 10, 2026.** .NET 10 is the LTS release,
supported to November 2028. So staying on `net8.0` means shipping on an unsupported runtime, and
.NET 9 is no answer at all: it expires the same day as the version it would replace.

The complication is that DialogueDown does not control the runtime its primary consumer uses. The
Godot documentation states it plainly:

> Godot bundles the parts of .NET needed to run already-compiled games.

An exported Godot game runs on the runtime **Godot** ships, not the one the developer built with.
Godot 4.4+ baselines on .NET 8, and its move to .NET 10 is still in progress. So the two clocks run
independently: ours is a support deadline, Godot's is a release schedule, and neither waits for the
other.

Picking a single target framework forces a choice between them:

| Single target | Cost |
| --- | --- |
| `net8.0` | Unsupported after November 10, 2026. |
| `net10.0` | Every Godot project must move the day we do — and it cannot until Godot does. |

## The shape

Neither, then. The libraries a game references **multi-target**, and everything else takes LTS:

| Project | Framework | Why |
| --- | --- | --- |
| `DialogueDown` | `net8.0;net10.0` | Referenced by a game; must load on Godot's bundled runtime. |
| `DialogueDown.ConfigurationLoader` | `net8.0;net10.0` | Same — a game reads its own `dialogue.toml`. |
| `DialogueDown.Cli` | `net10.0` | A developer tool. It never enters a game export, so nothing about Godot constrains it. |
| `DialogueDown.Visualization`, `.Live` | `net10.0` | Diagnostics only, not shipped in the core package. |

The published package carries both builds — `lib/net8.0/` and `lib/net10.0/` — and NuGet picks per
consumer. A Godot project on `net8.0` resolves the `net8.0` assembly and never learns the other
exists; a project on `net10.0` gets the LTS build. **The choice moves to the consumer, which is the
only place that knows which runtime it will run on.**

## Key design decisions

### D1 — Multi-target the consumer surface, not everything

Multi-targeting is not free: every target is another build and another set of test runs, and any
API a target lacks has to be `#if`-guarded. So it is spent only where it buys something — the two
libraries a game actually references. Applying it to the CLI or the visualizer would double their
build for a compatibility nobody consumes.

### D2 — The CLI takes LTS immediately

`DialogueDown.Cli` is the only artifact published today, and it is the one with genuine EOL
exposure. It is also a developer tool: it compiles and visualizes scripts on a developer's machine
and never ships inside a game, so moving it to `net10.0` cannot break an export. Its package
therefore carries `tools/net10.0` alone.

### D3 — The SDK floor is not the consumer floor

`global.json` pins SDK 10.0.0 because building `net10.0` requires it. That is a **contributor**
requirement and says nothing about consumers: the SDK builds every target the solution declares,
including `net8.0`. Conflating the two is the mistake this note exists to prevent — which is why
`global.json` now says so in its own comment.

## Boundary cases

| Case | Behavior |
| --- | --- |
| A Godot project on Godot's bundled .NET 8 | Resolves `lib/net8.0/`; unaffected by anything here. |
| A consumer already on .NET 10 | Resolves `lib/net10.0/` and gets the LTS runtime. |
| Running the test suite | The multi-targeted test projects run **twice**, once per framework, so a regression on either is caught. |
| CI | Installs the .NET 8 **and** .NET 10 runtimes: the SDK builds both targets, but executing the `net8.0` test run needs the .NET 8 runtime. |
| A contributor with only the .NET 8 SDK | Restore fails with a clear SDK-version error rather than a confusing target-framework one. |

## Testability

Dropping `net8.0` from a shipped library is the dangerous edit, because **nothing else would
notice**: the solution still builds, every test still passes, and only a consumer's Godot export
fails — somewhere else, later, for someone else. A guardrail test therefore reads the two shipped
project files directly and fails if either stops offering `net8.0` or `net10.0`, naming the
consequence in its message. It lives beside the other repository-manifest guards in
`dev-dotnet-tasks.test.mjs`.

## When Godot moves

Once Godot's bundled runtime reaches .NET 10, `net8.0` can be dropped from both libraries and this
note revised to record it. That is a one-line change per project plus the guardrail — the work of
carrying two targets was done here precisely so the eventual move is trivial.
