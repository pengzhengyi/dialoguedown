using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Fixing;

/// <summary>
/// One diagnostic as the report lists it, paired with what happened to its preferred fix — or
/// <c>null</c> when the diagnostic carries no fix.
/// </summary>
internal sealed record ReportedDiagnostic(LocatedDiagnostic Diagnostic, FixOutcome? Fix);
