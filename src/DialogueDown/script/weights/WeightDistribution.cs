namespace DialogueDown.Script.Weights;

/// <summary>
/// The result of normalizing a random choice's weights: one <see cref="Probabilities"/> value
/// per option, in order, summing to 1; and the <see cref="RawTotal"/> the explicit and
/// resolved auto weights added up to before normalization — so a caller can warn when the
/// author's numbers do not total 100.
/// </summary>
internal sealed record WeightDistribution(IReadOnlyList<double> Probabilities, double RawTotal);
