using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Edges;

public sealed class DivertEdgeTests
{
    [Fact]
    public void RoundTrip_ADivert_KeepsItsCondition()
    {
        const string Json = """
            {
              "kind": "divert",
              "target": 4,
              "condition": {
                "kind": "key",
                "key": "HasKey"
              }
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Edge, DivertEdge>(Json);
    }
}
