using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Reports each player-choice arm that carries nothing a menu could show for it, so a writer hears
/// about a blank row before a player is offered one.
/// </summary>
/// <remarks>
/// No label is made up for such an arm; it stays unlabelled and is reported. Only a player choice
/// is checked — a random arm is picked by the engine and never shown, so it carries a weight
/// rather than words.
/// </remarks>
internal static class ChoiceLabelValidator
{
    /// <summary>
    /// Checks every arm in <paramref name="options"/>, reporting each one with nothing to show
    /// into <paramref name="diagnostics"/> and carrying on so a writer hears about all of them.
    /// </summary>
    public static void Validate(IEnumerable<Choice> options, IDiagnosticSink diagnostics)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (var option in options.Where(option => option.Label().Count == 0))
        {
            diagnostics.Report(new Diagnostic(DiagnosticCatalog.OptionWithNothingToShow, option.Span, []));
        }
    }
}
