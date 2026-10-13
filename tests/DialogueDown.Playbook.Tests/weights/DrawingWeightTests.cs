using System.Globalization;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Playbook.Tests.Weights;

public sealed class DrawingWeightTests
{
    [Fact]
    public void Number_KeepsItsPercentage() =>
        Assert.Equal(30, new DrawingWeight.Number(30).Percentage);

    [Theory]
    [InlineData(-10)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Number_NoShareCanBe_IsRefusedNamingTheNumber(double percentage)
    {
        void Unusable() => _ = new DrawingWeight.Number(percentage);

        var error = Assert.Throws<ArgumentOutOfRangeException>(Unusable);
        Assert.Contains(percentage.ToString(CultureInfo.InvariantCulture), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Equality_ComparesTheKindAndTheNumber()
    {
        Assert.Equal(new DrawingWeight.Number(30), new DrawingWeight.Number(30));
        Assert.Equal(new DrawingWeight.Auto(), new DrawingWeight.Auto());
        Assert.NotEqual(new DrawingWeight.Number(30), new DrawingWeight.Number(70));
        Assert.NotEqual<DrawingWeight>(new DrawingWeight.Number(0), new DrawingWeight.Auto());
    }
}
