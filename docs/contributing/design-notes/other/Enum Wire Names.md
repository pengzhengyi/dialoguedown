# Enum wire names

> [!IMPORTANT]
> Status: **in progress**. Every enum in a JSON contract pins its wire name with a
> hand-written converter, so no shipped build needs a .NET 9+ package. The
> `net8.0` target stays — only the out-of-band reference goes. Closes
> [#314](https://github.com/pengzhengyi/dialoguedown/issues/314); the target
> story remains [Target Frameworks](./Target%20Frameworks.md).

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

Today the pin is the .NET 9 attribute `[JsonStringEnumMemberName]`. On the
`net8.0` build it comes from the out-of-band `System.Text.Json` package —
**~800 KB** (660.8 KB `System.Text.Json` + 75.3 KB `System.IO.Pipelines` +
64.3 KB `System.Text.Encodings.Web`) carried by net8.0 consumers only, per
[#314](https://github.com/pengzhengyi/dialoguedown/issues/314). This note
replaces the attribute with one hand-written converter per enum.

**In scope:** the two enums, their converters, the tests that keep the wire
names and the strictness, the removal of both conditional package references,
and the docs that record the decision.

**Out of scope:** moving a wire name (none moves), third-party dependencies
(surveyed below and rejected), the `net10.0` build's in-box path (the wire
format is identical), and dropping `net8.0` — that waits on Godot 4.8, as
[Target Frameworks](./Target%20Frameworks.md) records.

## Functionality checklist

- [ ] `SpeechStyle` and `Verdict` read and write the exact names `italic`,
      `bold`, `strikethrough`, `accept`, `refuse`.
- [ ] One implementation serves both targets; no `[JsonStringEnumMemberName]`
      usage remains.
- [ ] Both conditional `System.Text.Json` `PackageReference`s and the central
      `PackageVersion` entry are gone, so no package reference is
      target-conditional.
- [ ] A number, an unknown name, and a wrong-case name are rejected with a
      message naming the valid ones.
- [ ] An enum member added without a wire name fails loudly, and the
      exhaustiveness test catches it.
- [ ] `StringOnlyEnumConverter` is deleted — its strictness is inherent now.
- [ ] Existing JSON assertions pass unchanged, proving the wire format is
      byte-identical.

## Prior art

Surveyed 2026-09-24 (NuGet metadata and repository activity):

| Option | License | Evidence | Verdict |
| --- | --- | --- | --- |
| `[JsonStringEnumMemberName]` + out-of-band package | MIT | Works on every target; the ~800 KB above | The thing being replaced: a .NET 9 feature on a .NET 8 target, paid for in bytes. |
| **Hand-written converter (chosen)** | — | `SessionEntryJsonConverter` is the house pattern; ~55 lines for both enums | Explicit, zero footprint, strictness by construction. |
| [`Macross.Json.Extensions`](https://github.com/Macross-Software/core) 3.0.0 | MIT | 28.6M downloads; **last release 2022-06** | `[EnumMember]`-based converter; four years since a release; a dependency to replace a map. |
| [`EnumExtensions.System.Text.Json`](https://github.com/StefH/System.Text.Json.EnumExtensions) 1.3.0 | MIT | 3M downloads; updated 2025-10; 29 stars | A converter reading `[EnumMember]`/`[Display]`; single small maintainer; the same footprint objection. |
| [`PolySharp`](https://github.com/Sergio0694/PolySharp) 1.16 / [`Polyfill`](https://github.com/SimonCropp/Polyfill) 11.4 | MIT | Actively maintained polyfill generators | They can polyfill the attribute *type*, but the net8.0 serializer ignores it — the behavior is what is missing. |

Five members across two enums do not justify a dependency, an attribute swap,
and an upgrade surface. The hand-written route is also the fallback #314
already named.

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
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "italic" => SpeechStyle.Italic,
            "bold" => SpeechStyle.Bold,
            "strikethrough" => SpeechStyle.Strikethrough,
            var value => throw new JsonException(
                $"'{value}' is not a speech style; expected italic, bold, or strikethrough."),
        };

    public override void Write(
        Utf8JsonWriter writer, SpeechStyle value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            SpeechStyle.Italic => "italic",
            SpeechStyle.Bold => "bold",
            SpeechStyle.Strikethrough => "strikethrough",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        });
}
```

- `SpeechStyle` keeps the converter on the type —
  `[JsonConverter(typeof(SpeechStyleConverter))]` replaces
  `[JsonConverter(typeof(StringOnlyEnumConverter<SpeechStyle>))]`.
- `Verdict` keeps its current registration, in `ReadableFixture`'s options,
  swapping the in-box generic converter for `VerdictConverter`.
- Both enums lose their per-member attributes and gain a remark saying the map
  is the contract.
- `StringOnlyEnumConverter<TEnum>` has no other user, so it is deleted; the
  hand-written `Read` rejects numbers and unknown names by construction, which
  is what the wrapper's `allowIntegerValues: false` was for.

## Key design decisions

### D1 — Hand-write it; do not depend on it

Five members do not buy a dependency. The packages in the survey either predate
the problem (Macross), are small enough to vet forever (EnumExtensions), or
cannot supply the behavior at all (the polyfills). A map in our own source is
smaller than the vetting it replaces.

### D2 — The map is the contract, and a test proves it

The attribute's whole purpose was that a rename cannot change the format. The
converter keeps that: the wire names exist only as string literals in the
converter and as assertions in its tests, so a rename either keeps the literal
or fails the test.

### D3 — Strictness by construction

The current reader accepts a name and nothing else. The hand-written `Read`
only matches the known literals, so a number, a wrong-case name, or an unknown
name throws with a message naming the valid values. No `allowIntegerValues`
knob is needed because nothing lenient is reachable.

### D4 — One converter per enum, not a map registry

A generic `NamedEnumConverter<TEnum>` plus per-enum dictionaries is possible,
but two small switches read better, produce the exact error copy, and match the
existing one-converter-per-shape pattern. The write switch's default arm throws,
so a new member added without a wire name fails on first use; the
exhaustiveness test catches it earlier, at test time.

### D5 — Keep multi-targeting; only the reference goes

With the converter, the `net8.0` lane carries no out-of-band package — the cost
that made it expensive is gone, and what remains is build and test time. The
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
| `null` in a nullable position | Handled by the serializer before the converter, unchanged from today. |
| A new member with no wire name | The write switch's default arm throws; the exhaustiveness test fails first. |
| A converter registered twice (attribute and options) | Not done: each enum has exactly one registration site, as today. |
| A name reused by two members | Impossible to express in a switch; the compiler rejects the duplicate arm. |

## Demonstrable runs

At implementation, capture:

1. `dotnet list src/DialogueDown.Playbook/DialogueDown.Playbook.csproj package
   --include-transitive --framework net8.0` **before** (carrying
   `System.Text.Json`, `System.IO.Pipelines`, `System.Text.Encodings.Web`) and
   **after** (none).
2. The existing wire-format tests — `StyledTextFragmentTests` asserting
   `"style": "italic"` and `ReadableFixtureTests` asserting `"verdict":
   "accept"` — passing unchanged, which is the byte-identical proof.

## Integration

| File | Change |
| --- | --- |
| `src/DialogueDown.Playbook/speech/SpeechStyleConverter.cs` | New. |
| `src/DialogueDown.Playbook/speech/SpeechStyle.cs` | Converter attribute swapped; attributes removed; remark updated. |
| `src/DialogueDown.Playbook/speech/StringOnlyEnumConverter.cs` | Deleted. |
| `src/DialogueDown.Playbook/DialogueDown.Playbook.csproj` | Conditional `System.Text.Json` reference and its comment removed. |
| `tests/DialogueDown.Conformance/Verdict.cs` | `VerdictConverter` added beside it; attributes removed. |
| `tests/DialogueDown.Conformance/ReadableFixture.cs` | Converter registration swapped. |
| `tests/DialogueDown.Conformance/DialogueDown.Conformance.csproj` | Conditional reference and comment removed. |
| `Directory.Packages.props` | `System.Text.Json` `PackageVersion` removed. |
| [Playbook format](../runtime/Playbook%20Format.md) | The wire-name decision points here for how names are pinned. |
| [Target Frameworks](./Target%20Frameworks.md) | The reference is gone; "When Godot moves" now covers only dropping `net8.0`. |

No changelog entry: the libraries are not published and no wire name or public
API changes.

## Testability

`SpeechStyleConverterTests` and `VerdictConverterTests`:

- Every member round-trips through `PlaybookJson.Options` (and the fixture
  options for `Verdict`).
- Serialization writes the exact literal names — the rename guard.
- `"Italic"`, `"cursive"`, `1`, and `null` are rejected (the last by the
  serializer where nullable).
- The exhaustiveness test: every `Enum.GetValues<T>()` member maps to a name
  the converter writes.

The Conformance suite already asserts the corpus's own names
(`ReadableFixtureTests`), so a drifted name fails there too.

## Open questions and deferred work

- **`DialogueDown.Runtime` is missing from the guardrail.** The manifest guard
  in `dev-dotnet-tasks.test.mjs` and the Target Frameworks table cover
  `DialogueDown`, `ConfigurationLoader`, and `Playbook`, but `Runtime` also
  multi-targets. Confirm whether a game references it, and if so add it to both
  — separately from this change.
- **Drop `net8.0`** when Godot 4.8 ships
  ([#123738](https://github.com/godotengine/godot/pull/123738)); nothing here
  blocks that, and nothing here should anticipate it.
