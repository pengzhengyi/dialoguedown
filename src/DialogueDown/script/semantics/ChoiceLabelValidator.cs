using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Reports each player-choice arm that carries nothing a menu could show for it, so a writer hears
/// about a blank row before a player is offered one.
/// </summary>
/// <remarks>
/// The compile invents nothing for such an arm: reading words off the node it leads to would put
/// somebody else's line in the player's mouth, so an unlabelled arm stays unlabelled and this says
/// so instead. Only a player choice is checked — a random arm is picked by the engine and never
/// shown, so it carries a weight rather than words.
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
