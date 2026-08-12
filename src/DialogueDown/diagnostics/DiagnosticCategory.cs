namespace DialogueDown.Diagnostics;

/// <summary>
/// The kind of rule a <see cref="DiagnosticDescriptor"/> belongs to. Each category owns a
/// range of <c>DLG####</c> codes — <see cref="Syntax"/> is <c>DLG1xxx</c>,
/// <see cref="Semantic"/> is <c>DLG2xxx</c>, <see cref="Style"/> is <c>DLG3xxx</c> — so a
/// code's range names its category, and a descriptor checks that the two agree. Part of the public
/// diagnostic view (<see cref="LocatedDiagnostic"/>), so its members carry explicit, stable values.
/// </summary>
public enum DiagnosticCategory
{
    /// <summary>
    /// A problem in the script's surface: text that does not parse as intended, or Markdown that
    /// never becomes dialogue.
    /// </summary>
    Syntax = 0,

    /// <summary>A meaning-level problem: references that do not resolve, conflicts.</summary>
    Semantic = 1,

    /// <summary>A stylistic or advisory note: the script is valid but could read better.</summary>
    Style = 2,
}
