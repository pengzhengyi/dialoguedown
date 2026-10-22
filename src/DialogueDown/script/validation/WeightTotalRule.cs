using System.Globalization;
using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Weights;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Checks a random choice's number weights. It asks the <see cref="IWeightNormalization"/>
/// strategy for the raw total, then reports at the group's start: an error
/// (<see cref="DiagnosticCatalog.ZeroChoiceWeightTotal"/>) when the weights sum to zero, so no
/// option can be selected; otherwise a warning
/// (<see cref="DiagnosticCatalog.ChoiceWeightsNotOneHundred"/>) when they do not total
/// approximately 100%. A single-option group is spared the warning, because its lone option is
/// always selected while its weight is above zero, but a lone option with a <c>`0%`</c> chance is
/// never selected, so its zero total is still an error. A group containing a <see cref="QueryWeight"/>
/// or a conditional option is skipped: the achievable total is only known at runtime.
/// </summary>
internal sealed class WeightTotalRule(IWeightNormalization normalization) : IDiagnosticRule
{
    private const double HundredPercent = 100;
    private const double NearHundredTolerance = 0.5;
    private const double NearZeroThreshold = 1e-9;

    public void Check(DialogueTreeIndex nodes, IDiagnosticSink diagnostics)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (var random in nodes.OfType<RandomChoices>())
        {
            Inspect(random, diagnostics);
        }
    }

    /// <summary>Whether a static total is zero, so no option can be selected.</summary>
    /// <param name="total">The total of a random choice's number weights.</param>
    /// <returns><see langword="true"/> when the total is zero.</returns>
    internal static bool IsZeroTotal(double total) => total < NearZeroThreshold;

    private void Inspect(RandomChoices random, IDiagnosticSink diagnostics)
    {
        // A query weight has no compile-time value, and a conditional option may be excluded, so
        // the achievable total is only known at runtime — the runtime resolves and re-normalizes it.
        if (random.Options.Any(option => option.Weight is QueryWeight || option.IsConditional()))
        {
            return;
        }

        var total = normalization
            .Normalize(random.Options.Select(option => option.Weight).ToList())
            .RawTotal;
        var location = SourceSpan.EmptyAt(random.Span.Start);

        if (IsZeroTotal(total))
        {
            diagnostics.Report(new Diagnostic(DiagnosticCatalog.ZeroChoiceWeightTotal, location, []));
        }
        else if (random.Options.Count > 1 && Math.Abs(total - HundredPercent) > NearHundredTolerance)
        {
            diagnostics.Report(new Diagnostic(
                DiagnosticCatalog.ChoiceWeightsNotOneHundred,
                location,
                [total.ToString("0.##", CultureInfo.InvariantCulture)]));
        }
    }
}
