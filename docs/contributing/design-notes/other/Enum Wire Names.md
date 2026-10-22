# Enum Wire Names

> [!NOTE]
> Status: **implemented**. Every enum in a JSON contract pins its wire name with a
> hand-written converter, so no shipped build needs a .NET 9+ package. The
> `net8.0` target stays, with no out-of-band package. The target story is
> [Target Frameworks](./Target%20Frameworks.md).

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Functionality checklist](#functionality-checklist)
- [Prior art](#prior-art)
- [The converter](#the-converter)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Demonstrable runs](#demonstrable-runs)
- [Integration](#integration)
- [Testability](#testability)
- [Open questions and deferred work](#open-questions-and-deferred-work)

## Goal and scope

The JSON contracts pin every enum's wire name explicitly, so renaming a C#
member cannot silently change a format:

- `SpeechStyle` — `italic`, `bold`, `strikethrough` — in the playbook a game
  reads.
- `Verdict` — `accept`, `refuse` — in the conformance corpus other runtimes
  read.

The pin is one hand-written converter per enum. The alternative — the .NET 9
attribute `[JsonStringEnumMemberName]` — would make the `net8.0` build carry
an out-of-band `System.Text.Json` package: **~800 KB** (660.8 KB
`System.Text.Json` + 75.3 KB `System.IO.Pipelines` + 64.3 KB
`System.Text.Encodings.Web`) for net8.0 consumers only.

**In scope:** the two enums, their converters, the tests that keep the wire
names and the strictness, package references that are never target-conditional,
and the docs that record the decision.

**Out of scope:** moving a wire name (none moves), third-party dependencies
(surveyed below and rejected), the `net10.0` build's in-box path (the wire
format is identical), and dropping `net8.0` — that waits on Godot 4.8, as
[Target Frameworks](./Target%20Frameworks.md) records.

## Functionality checklist

- [x] `SpeechStyle` and `Verdict` read and write the exact names `italic`,
      `bold`, `strikethrough`, `accept`, `refuse`.
- [x] One implementation serves both targets; no `[JsonStringEnumMemberName]`
      is used.
- [x] No project references `System.Text.Json` as a package and
      `Directory.Packages.props` has no version for it, so no package reference
      is target-conditional.
- [x] A number, an unknown name, and a wrong-case name are rejected with a
      message naming the valid ones.
- [x] An enum member added without a wire name fails loudly, and the
      exhaustiveness test catches it.
- [x] The wire format is the one the attribute wrote: `StyledTextFragmentTests`
      and `ReadableFixtureTests` pin it.

## Prior art

Surveyed 2026-09-24 (NuGet metadata and repository activity):

| Option | License | Evidence | Verdict |
| --- | --- | --- | --- |
| `[JsonStringEnumMemberName]` + out-of-band package | MIT | Works on every target; the ~800 KB above | Rejected: a .NET 9 feature on a .NET 8 target, paid for in bytes. |
| **Hand-written converter (chosen)** | — | `SessionEntryJsonConverter` is the house pattern; ~55 lines for both enums | Explicit, zero footprint, strictness by construction. |
| [`Macross.Json.Extensions`](https://github.com/Macross-Software/core) 3.0.0 | MIT | 28.6M downloads; **last release 2022-06** | `[EnumMember]`-based converter; four years since a release; a dependency to replace a map. |
| [`EnumExtensions.System.Text.Json`](https://github.com/StefH/System.Text.Json.EnumExtensions) 1.3.0 | MIT | 3M downloads; updated 2025-10; 29 stars | A converter reading `[EnumMember]`/`[Display]`; single small maintainer; the same footprint objection. |
| [`PolySharp`](https://github.com/Sergio0694/PolySharp) 1.16 / [`Polyfill`](https://github.com/SimonCropp/Polyfill) 11.4 | MIT | Actively maintained polyfill generators | They can polyfill the attribute *type*, but the net8.0 serializer ignores it — the behavior is what is missing. |

Five members across two enums do not justify a dependency, an attribute swap,
and more packages to upgrade.

## The converter

The house pattern is
[`SessionEntryJsonConverter`](https://github.com/pengzhengyi/dialoguedown/blob/main/tests/DialogueDown.Conformance/SessionEntry.cs):
a sealed `internal` class, a `Read` that switches over the JSON shape with a
precise `JsonException`, and a `Write` that either writes exactly or throws.
Each enum gets one converter in the same shape:

```csharp
/// <summary>Reads and writes <see cref="SpeechStyle"/> by its pinned wire names.</summary>
internal sealed class SpeechStyleConverter : JsonConverter<SpeechStyle>
{
    public override SpeechStyle Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"A speech style is a string, not {reader.TokenType}.");
        }

        return reader.GetString() switch
        {
            "italic" => SpeechStyle.Italic,
            "bold" => SpeechStyle.Bold,
            "strikethrough" => SpeechStyle.Strikethrough,
            var value => throw new JsonException(
                $"'{value}' is not a speech style. Use italic, bold, or strikethrough."),
        };
    }

    public override void Write(
        Utf8JsonWriter writer, SpeechStyle value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            SpeechStyle.Italic => "italic",
            SpeechStyle.Bold => "bold",
            SpeechStyle.Strikethrough => "strikethrough",
            _ => throw new ArgumentOutOfRangeException(
                nameof(value), value, "No wire name is defined for this speech style."),
        });
}
```

The `Read` checks the token type before switching on the name: `GetString()`
throws `InvalidOperationException` on a number, and the contract is a
`JsonException`. `VerdictConverter` is the same shape, in its own file beside
the enum — StyleCop's file-name rule skips enums when it picks a file's first
type, so a converter sharing `Verdict.cs` would name the file against itself.

- `SpeechStyle` carries its converter on the type:
  `[JsonConverter(typeof(SpeechStyleConverter))]`.
- `Verdict` registers `VerdictConverter` in `ReadableFixture`'s options.
- Neither enum has per-member attributes; each has a remark saying its members'
  wire names are fixed, and where.
- The hand-written `Read` rejects numbers and unknown names by construction, so
  no wrapper converter with `allowIntegerValues: false` is needed.

## Key design decisions

### D1 — Hand-write it; do not depend on it

Five members do not buy a dependency. The packages in the survey either predate
the problem (Macross), are small enough to vet forever (EnumExtensions), or
cannot supply the behavior at all (the polyfills). A map in our own source is
smaller than the vetting it replaces.

### D2 — The map is the contract, and a test proves it

A pinned wire name exists so a rename cannot change the format. The converter
keeps that: the wire names exist only as string literals in the
converter and as assertions in its tests, so a rename either keeps the literal
or fails the test.

### D3 — Strictness by construction

The reader accepts a name and nothing else. The hand-written `Read`
only matches the known literals, so a number, a wrong-case name, or an unknown
name throws with a message naming the valid values. No `allowIntegerValues`
knob is needed because nothing lenient is reachable.

### D4 — One converter per enum, not a map registry

A generic `NamedEnumConverter<TEnum>` plus per-enum dictionaries is possible,
but two small switches read better, produce the exact error copy, and match the
existing one-converter-per-shape pattern. The write switch's default case throws,
so a new member added without a wire name fails on first use; the
exhaustiveness test catches it earlier, at test time.

### D5 — Keep multi-targeting, without the reference

With the converter, the `net8.0` lane carries no out-of-band package, so what it
costs is build and test time. The
lane still matters: a net10.0 consumer can resolve a net8.0 asset, but a net8.0
consumer (Godot ≤ 4.7) cannot resolve a net10.0-only package. Dropping
`net8.0` stays a separate decision, triggered by Godot 4.8 shipping its net10.0
minimum
([Godot PR #123738](https://github.com/godotengine/godot/pull/123738)),
as [Target Frameworks](./Target%20Frameworks.md) records.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| Unknown name (`"cursive"`) | `JsonException` naming the value and the valid names. |
| Wrong case (`"Italic"`) | Same rejection; the format is case-sensitive by design. |
| Number (`"style": 1`) | Rejected in `Read`; the schema also disallows it. |
| `null` in a nullable position | Handled by the serializer before the converter. |
| A new member with no wire name | The write switch's default case throws; the exhaustiveness test fails first. |
| A converter registered twice (attribute and options) | Not done: each enum has exactly one registration site. |
| A name reused by two members | Impossible to express in a switch; the compiler rejects the duplicate case. |

## Demonstrable runs

Neither project's `net8.0` target pulls in any of the three out-of-band
assemblies:

```console
$ dotnet list src/DialogueDown.Playbook/DialogueDown.Playbook.csproj package \
    --include-transitive --framework net8.0 \
    | grep -E 'System\.(Text\.Json|IO\.Pipelines|Text\.Encodings\.Web)'
$ # no output; the same holds for tests/DialogueDown.Conformance
```

The wire format is the one the attribute wrote: `StyledTextFragmentTests`
(`"style": "italic"`) and `ReadableFixtureTests` (`"verdict": "accept"`) pin it.

## Integration

| File | Role |
| --- | --- |
| `src/DialogueDown.Playbook/speech/SpeechStyleConverter.cs` | The playbook's converter. |
| `src/DialogueDown.Playbook/speech/SpeechStyle.cs` | Names the converter in `[JsonConverter]`; its remark says the wire names are fixed. |
| `tests/DialogueDown.Conformance/VerdictConverter.cs` | The corpus's converter, in its own file for the SA1649 reason above. |
| `tests/DialogueDown.Conformance/Verdict.cs` | Its remark points at the converter. |
| `tests/DialogueDown.Conformance/ReadableFixture.cs` | Registers `VerdictConverter`. |
| [Playbook format](../runtime/Playbook%20Format.md) | The wire-name decision points here for how names are pinned. |
| [Target Frameworks](./Target%20Frameworks.md) | "When Godot moves" covers dropping `net8.0`. |

## Testability

`SpeechStyleConverterTests` covers the shipping enum:

- Every member round-trips through `PlaybookJson.Options`.
- Serialization writes the exact literal names — the rename guard — and the
  exhaustive check fails if the enum gains a member without one.
- `"Italic"`, `"cursive"`, and `1` are refused; `null` is allowed where the
  position is nullable.

`Verdict` has no test file of its own: `ReadableFixtureTests` already pins both
of its names, the wrong case, an unknown name, and a number through the fixture
reader — the same public path that covers `SessionEntryJsonConverter`, and the
reader is the only place a verdict is consumed.

## Open questions and deferred work

- **`DialogueDown.Runtime` is missing from the guardrail.** The manifest guard
  in `dev-dotnet-tasks.test.mjs` covers `DialogueDown`, `ConfigurationLoader`, and
  `Playbook`, but not `Runtime`, which a game references and which multi-targets
  too; [Target Frameworks](./Target%20Frameworks.md) records the gap.
- **Drop `net8.0`** when Godot 4.8 ships
  ([#123738](https://github.com/godotengine/godot/pull/123738)); nothing here
  blocks that, and nothing here should anticipate it.
