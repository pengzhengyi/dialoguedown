namespace DialogueDown.Diagnostics;

/// <summary>
/// The public description of one diagnostic code: its <see cref="Code"/>, a short
/// <see cref="Title"/>, its <see cref="Category"/>, and the <see cref="DefaultSeverity"/> its
/// diagnostics take.
/// </summary>
/// <param name="Code">The stable <c>DLG####</c> identifier.</param>
/// <param name="Title">A short human title for the code.</param>
/// <param name="Category">The kind of rule the code belongs to.</param>
/// <param name="DefaultSeverity">The severity the code's diagnostics take.</param>
public sealed record DiagnosticCodeInfo(
    string Code,
    string Title,
    DiagnosticCategory Category,
    DiagnosticSeverity DefaultSeverity)
{
    /// <summary>
    /// Whether a project may disable this code: true for an <see cref="DiagnosticSeverity.Info"/>
    /// or <see cref="DiagnosticSeverity.Warning"/> code, false for an
    /// <see cref="DiagnosticSeverity.Error"/>, which would let a broken script compile.
    /// </summary>
    public bool IsOptional => DefaultSeverity != DiagnosticSeverity.Error;
}
