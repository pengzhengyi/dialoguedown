using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Nodes;

public sealed class BranchNodeTests
{
    [Fact]
    public void RoundTrip_ABranch_CarriesOnlyItsArms()
    {
        // The arms hold the conditions and their order; the branch itself only fans out.
        const string Json = """
            {
              "kind": "branch",
              "id": 5,
              "out": [
                {
                  "kind": "branch",
                  "target": 6,
                  "order": 0,
                  "condition": {
                    "kind": "key",
                    "key": "IsAngry"
                  }
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Node, BranchNode>(Json);
    }
}
