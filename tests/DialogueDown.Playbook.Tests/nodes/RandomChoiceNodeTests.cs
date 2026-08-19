using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class RandomChoiceNodeTests
{
    [Fact]
    public void RoundTrip_ARandomChoice_CarriesOnlyItsArms()
    {
        const string Json = """
            {
              "kind": "random-choice",
              "id": 3,
              "out": [
                {
                  "kind": "random-option",
                  "target": 4,
                  "weight": {
                    "kind": "auto"
                  }
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Node, RandomChoiceNode>(Json);
    }
}
