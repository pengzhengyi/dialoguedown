using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Regions;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class RegionIdSequenceTests
{
    [Fact]
    public void Next_StartsAtZeroAndIncrements()
    {
        var sequence = new RegionIdSequence();

        Assert.Equal(new RegionId(0), sequence.Next());
        Assert.Equal(new RegionId(1), sequence.Next());
        Assert.Equal(new RegionId(2), sequence.Next());
    }
}
