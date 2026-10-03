using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Nodes;

public sealed class ChoiceNodeTests
{
    [Fact]
    public void RoundTrip_AChoice_KeepsWhetherItIsOrdered()
    {
        // The node lists no condition keys: a runner gathers them from the options to resolve the
        // whole menu in one ask.
        const string Json = """
            {
              "kind": "choice",
              "id": 1,
              "out": [
                {
                  "kind": "option",
                  "target": 2,
                  "label": [
                    {
                      "kind": "text",
                      "text": "Ask about the inn"
                    }
                  ],
                  "condition": {
                    "kind": "key",
                    "key": "IsCurious"
                  }
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Node, ChoiceNode>(Json);
    }
}
