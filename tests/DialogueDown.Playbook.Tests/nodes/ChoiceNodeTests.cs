using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class ChoiceNodeTests
{
    [Fact]
    public void RoundTrip_AChoice_KeepsWhetherItIsOrdered()
    {
        // A runner still resolves the whole menu in one ask: it gathers the keys by walking the
        // options it just arrived at, rather than reading a list the playbook repeated.
        const string Json = """
            {
              "kind": "choice",
              "id": 1,
              "ordered": false,
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
