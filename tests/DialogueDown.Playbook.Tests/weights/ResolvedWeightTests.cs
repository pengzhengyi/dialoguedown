using System.Globalization;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Playbook.Tests.Weights;

public sealed class ResolvedWeightTests
{
    [Fact]
    public void Number_KeepsItsPercentage() =>
        Assert.Equal(30, new ResolvedWeight.Number(30).Percentage);

    [Theory]
    [InlineData(-10)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Number_NoShareCanBe_IsRefusedNamingTheNumber(double percentage)
    {
        void Unusable() => _ = new ResolvedWeight.Number(percentage);

        var error = Assert.Throws<ArgumentOutOfRangeException>(Unusable);
        Assert.Contains(percentage.ToString(CultureInfo.InvariantCulture), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Equality_ComparesTheKindAndTheNumber()
    {
        Assert.Equal(new ResolvedWeight.Number(30), new ResolvedWeight.Number(30));
        Assert.Equal(new ResolvedWeight.Auto(), new ResolvedWeight.Auto());
        Assert.NotEqual(new ResolvedWeight.Number(30), new ResolvedWeight.Number(70));
        Assert.NotEqual<ResolvedWeight>(new ResolvedWeight.Number(0), new ResolvedWeight.Auto());
    }
}
