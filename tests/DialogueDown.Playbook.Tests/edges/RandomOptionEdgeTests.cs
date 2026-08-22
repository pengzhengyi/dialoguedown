using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Edges;

public sealed class RandomOptionEdgeTests
{
    [Fact]
    public void RoundTrip_ARandomOption_KeepsItsWeight()
    {
        // No label: the engine draws this one, so nobody is shown a menu.
        const string Json = """
            {
              "kind": "random-option",
              "target": 5,
              "weight": {
                "kind": "number",
                "percentage": 25
              }
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Edge, RandomOptionEdge>(Json);
    }

    [Fact]
    public void Construct_WithoutAWeight_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new RandomOptionEdge(5, null!, Condition: null));
    }
}
