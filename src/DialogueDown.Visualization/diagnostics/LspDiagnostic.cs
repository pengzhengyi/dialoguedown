using System.Text.Json.Serialization;
using DialogueDown.Visualization.Lsp;

namespace DialogueDown.Visualization.Diagnostics;

/// <summary>
/// One diagnostic in the shape the Language Server Protocol defines, so the value the report
/// payload carries is one a language server could publish unchanged. It carries a zero-based
/// <see cref="Range"/>, an integer
/// <see cref="Severity"/>, the diagnostic's <see cref="Code"/> and rendered <see cref="Message"/>,
/// the producing <see cref="Source"/> (<c>"dialoguedown"</c>), and the suggested repairs
/// <see cref="Fixes"/> (absent when the compiler knows none). Projected from the core
/// <see cref="DialogueDown.Diagnostics.LocatedDiagnostic"/> by <see cref="DiagnosticProjection"/>.
/// </summary>
/// <remarks>
/// The report serializer writes enums as strings by default, so <see cref="Severity"/> carries a
/// property-level <see cref="JsonNumberEnumConverter{TEnum}"/> to keep it the protocol's integer on
/// the wire — a property-level converter is the only kind that overrides one in the serializer's
/// converter collection.
/// </remarks>
internal sealed record LspDiagnostic(
    LspRange Range,
    [property: JsonConverter(typeof(JsonNumberEnumConverter<LspSeverity>))] LspSeverity Severity,
    string Code,
    string Message,
    string Source,
    IReadOnlyList<LspFix>? Fixes = null);
