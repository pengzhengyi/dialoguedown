using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Nodes;

public sealed class EndNodeTests
{
    [Fact]
    public void RoundTrip_TheEnd_LeadsNowhere()
    {
        const string Json = """
            {
              "kind": "end",
              "id": 9,
              "out": []
            }
            """;

        var end = PlaybookJsonAssert.AssertRoundTrip<Node, EndNode>(Json);

        Assert.Empty(end.Out);
    }
}
