using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Edges;

public sealed class SuccessionEdgeTests
{
    [Fact]
    public void RoundTrip_Succession_IsJustAKindAndATarget()
    {
        // Reading order: what plays next when nothing branches. It is the only edge with no
        // condition, because falling through is not a decision.
        const string Json = """
            {
              "kind": "succession",
              "target": 1
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Edge, SuccessionEdge>(Json);
    }

    [Fact]
    public void Construct_NegativeTarget_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SuccessionEdge(-1));
    }
}
