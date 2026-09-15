using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Edges;

public sealed class DivertEdgeTests
{
    [Fact]
    public void RoundTrip_AConditionalDivert_KeepsItsLabelAndCondition()
    {
        // The label is what the writer called the jump. It survives nowhere else, because a jump
        // is written inside a line but is no part of what that line says.
        const string Json = """
            {
              "kind": "divert",
              "target": 4,
              "label": [
                {
                  "kind": "text",
                  "text": "the inn"
                }
              ],
              "condition": {
                "kind": "key",
                "key": "HasKey"
              }
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Edge, DivertEdge>(Json);
    }

    [Fact]
    public void RoundTrip_ADivertNobodyNamed_KeepsAnEmptyLabel()
    {
        const string Json = """
            {
              "kind": "divert",
              "target": 4,
              "label": []
            }
            """;

        var divert = PlaybookJsonAssert.AssertRoundTrip<Edge, DivertEdge>(Json);

        Assert.Null(divert.Condition);
    }

    [Fact]
    public void Equality_EqualLabels_AreEqual()
    {
        var left = new DivertEdge(4, [new TextFragment("the inn")], Condition: null);
        var right = new DivertEdge(4, [new TextFragment("the inn")], Condition: null);

        EqualityAssert.AssertValueEqual(left, right);
    }

    [Fact]
    public void Equality_DifferentLabels_AreNotEqual()
    {
        var left = new DivertEdge(4, [new TextFragment("the inn")], Condition: null);
        var right = new DivertEdge(4, [new TextFragment("the market")], Condition: null);

        EqualityAssert.AssertValueUnequal(left, right);
    }
}
