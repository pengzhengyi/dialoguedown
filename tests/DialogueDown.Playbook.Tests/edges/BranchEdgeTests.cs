using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Edges;

public sealed class BranchEdgeTests
{
    [Fact]
    public void RoundTrip_AGatedBranch_KeepsItsCondition()
    {
        const string Json = """
            {
              "kind": "branch",
              "target": 7,
              "condition": {
                "kind": "key",
                "key": "IsAngry"
              }
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Edge, BranchEdge>(Json);
    }

    [Fact]
    public void RoundTrip_AnElse_IsJustAKindAndATarget()
    {
        const string Json = """
            {
              "kind": "branch",
              "target": 7
            }
            """;

        var branch = PlaybookJsonAssert.AssertRoundTrip<Edge, BranchEdge>(Json);

        Assert.Null(branch.Condition);
    }
}
