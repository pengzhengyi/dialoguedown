using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Nodes;

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

    [Fact]
    public void Equality_EqualEffects_AreEqual()
    {
        var left = new ControlNode(7, [new CustomCommandFragment("JoinClub", ["Alice"])], Condition: null, []);
        var right = new ControlNode(7, [new CustomCommandFragment("JoinClub", ["Alice"])], Condition: null, []);

        EqualityAssert.AssertValueEqual(left, right);
    }

    [Fact]
    public void Equality_DifferentEffects_AreNotEqual()
    {
        var left = new ControlNode(7, [new CustomCommandFragment("JoinClub", ["Alice"])], Condition: null, []);
        var right = new ControlNode(7, [new CustomCommandFragment("JoinClub", ["Bob"])], Condition: null, []);

        EqualityAssert.AssertValueUnequal(left, right);
    }
}
