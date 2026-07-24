using System.Globalization;
using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Weights;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Checks a random choice's static weights. It asks the <see cref="IWeightNormalization"/>
/// strategy for the raw total, then reports at the group's start: an error
/// (<see cref="DiagnosticCatalog.ZeroChoiceWeightTotal"/>) when the weights sum to zero, so no
/// option can be selected; otherwise a warning
/// (<see cref="DiagnosticCatalog.ChoiceWeightsNotOneHundred"/>) when they do not total
/// approximately 100%. A single-option group is skipped: its lone option is always selected, so
/// its total is moot.
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

    private void Inspect(RandomChoices random, IDiagnosticSink diagnostics)
    {
        if (random.Options.Count <= 1)
        {
            return;
        }

        var total = normalization
            .Normalize(random.Options.Select(option => option.Weight).ToList())
            .RawTotal;
        var location = SourceSpan.EmptyAt(random.Span.Start);

        if (total < NearZeroThreshold)
        {
            diagnostics.Report(new Diagnostic(DiagnosticCatalog.ZeroChoiceWeightTotal, location, []));
        }
        else if (Math.Abs(total - HundredPercent) > NearHundredTolerance)
        {
            diagnostics.Report(new Diagnostic(
                DiagnosticCatalog.ChoiceWeightsNotOneHundred,
                location,
                [total.ToString("0.##", CultureInfo.InvariantCulture)]));
        }
    }
}
