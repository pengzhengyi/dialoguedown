using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class ControlNodeTests
{
    [Fact]
    public void RoundTrip_AnEffectOnlyLine_KeepsItsEffectsAndCondition()
    {
        // A silent command: no speaker, so nothing is ever attributed to a character.
        const string Json = """
            {
              "kind": "control",
              "id": 7,
              "effects": [
                {
                  "kind": "custom-command",
                  "name": "JoinClub",
                  "args": [
                    "Alice"
                  ]
                }
              ],
              "condition": {
                "kind": "key",
                "key": "IsMember"
              },
              "out": [
                {
                  "kind": "succession",
                  "target": 8
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Node, ControlNode>(Json);
    }
}
