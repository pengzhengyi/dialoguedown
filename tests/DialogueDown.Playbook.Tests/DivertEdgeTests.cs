using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

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
