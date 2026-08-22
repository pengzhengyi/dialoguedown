using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Edges;

public sealed class BranchEdgeTests
{
    [Fact]
    public void RoundTrip_ABranchArm_KeepsItsOrder()
    {
        // Order is what makes if/elseif/else mean what it says; a JSON array alone would not
        // oblige a reader to keep it.
        const string Json = """
            {
              "kind": "branch",
              "target": 7,
              "order": 1,
              "condition": {
                "kind": "key",
                "key": "IsAngry"
              }
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Edge, BranchEdge>(Json);
    }

    [Fact]
    public void Construct_NegativeOrder_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BranchEdge(7, -1, Condition: null));
    }
}
