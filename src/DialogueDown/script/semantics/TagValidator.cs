using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Checks each reserved (<c>##name</c>) tag against DialogueDown's <see cref="ReservedTagNames.Known"/>
/// set and reports one whose name is not recognized. Custom (<c>#name</c>) tags are opaque and
/// never reach here.
/// </summary>
internal static class TagValidator
{
    /// <summary>
    /// Validates every reserved tag in <paramref name="tags"/>, reporting each unknown one into
    /// <paramref name="diagnostics"/> and treating it as inert so the rest are still checked.
    /// </summary>
    public static void Validate(IEnumerable<ReservedTag> tags, IDiagnosticSink diagnostics)
    {
        foreach (var tag in tags)
        {
            if (!ReservedTagNames.Known.Contains(tag.Name))
            {
                diagnostics.Report(new Diagnostic(DiagnosticCatalog.UnknownReservedTag, tag.Span, [tag.Name]));
            }
        }
    }
}
