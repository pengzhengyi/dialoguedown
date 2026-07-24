using DialogueDown.Script.Ast;
using DialogueDown.Script.Weights;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Weights;

public sealed class DefaultWeightNormalizationTests
{
    private static readonly DefaultWeightNormalization _normalizer = new();

    [Fact]
    public void EqualNumbers_SplitEvenly_AndTotalOneHundred()
    {
        var result = _normalizer.Normalize([new NumberWeight(50), new NumberWeight(50)]);

        AssertProbabilities(result, 0.5, 0.5);
        NumericAssert.Equal(100, result.RawTotal);
    }

    [Fact]
    public void NumbersBelowOneHundred_AreNormalizedBySum_AndReportTheRawTotal()
    {
        var result = _normalizer.Normalize([new NumberWeight(30), new NumberWeight(30)]);

        AssertProbabilities(result, 0.5, 0.5);
        NumericAssert.Equal(60, result.RawTotal);
    }

    [Fact]
    public void NumbersAboveOneHundred_AreNormalizedBySum_AndReportTheRawTotal()
    {
        var result = _normalizer.Normalize([new NumberWeight(60), new NumberWeight(60)]);

        AssertProbabilities(result, 0.5, 0.5);
        NumericAssert.Equal(120, result.RawTotal);
    }

    [Fact]
    public void AnAutoWeight_ClaimsTheLeftoverAfterTheExplicitWeights()
    {
        var result = _normalizer.Normalize([new NumberWeight(70), new AutoWeight()]);

        AssertProbabilities(result, 0.7, 0.3);
        NumericAssert.Equal(100, result.RawTotal);
    }

    [Fact]
    public void SeveralAutoWeights_SplitTheLeftoverEqually()
    {
        var result = _normalizer.Normalize(
            [new NumberWeight(50), new AutoWeight(), new AutoWeight()]);

        AssertProbabilities(result, 0.5, 0.25, 0.25);
        NumericAssert.Equal(100, result.RawTotal);
    }

    [Fact]
    public void AllAutoWeights_ProduceAUniformDistribution()
    {
        var result = _normalizer.Normalize([new AutoWeight(), new AutoWeight()]);

        AssertProbabilities(result, 0.5, 0.5);
        NumericAssert.Equal(100, result.RawTotal);
    }

    [Fact]
    public void WhenExplicitWeightsExceedOneHundred_AutoWeightsResolveToZero()
    {
        var result = _normalizer.Normalize([new NumberWeight(120), new AutoWeight()]);

        AssertProbabilities(result, 1.0, 0.0);
        NumericAssert.Equal(120, result.RawTotal);
    }

    [Fact]
    public void AllZeroWeights_RecoverToAUniformDistribution()
    {
        // A zero total is a validation error (DLG2010); the strategy still returns a valid
        // distribution so downstream never divides by zero.
        var result = _normalizer.Normalize([new NumberWeight(0), new NumberWeight(0)]);

        AssertProbabilities(result, 0.5, 0.5);
        NumericAssert.Equal(0, result.RawTotal);
    }

    [Fact]
    public void ASingleOption_IsAlwaysSelected_ButItsRawTotalIsPreserved()
    {
        var result = _normalizer.Normalize([new NumberWeight(50)]);

        AssertProbabilities(result, 1.0);
        NumericAssert.Equal(50, result.RawTotal);
    }

    [Fact]
    public void NonIntegerWeights_AreNormalized_AndProbabilitiesSumToOne()
    {
        var result = _normalizer.Normalize(
            [new NumberWeight(33.3), new NumberWeight(33.3), new NumberWeight(33.3)]);

        AssertProbabilities(result, 1.0 / 3, 1.0 / 3, 1.0 / 3);
        NumericAssert.Equal(99.9, result.RawTotal);
    }

    [Fact]
    public void ANegativeWeight_IsRejected_AsACallerError() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _normalizer.Normalize([new NumberWeight(-10), new NumberWeight(50)]));

    private static void AssertProbabilities(WeightDistribution result, params double[] expected)
    {
        Assert.Equal(expected.Length, result.Probabilities.Count);
        NumericAssert.Equal(1, result.Probabilities.Sum());
        for (var i = 0; i < expected.Length; i++)
        {
            NumericAssert.Equal(expected[i], result.Probabilities[i]);
        }
    }
}
