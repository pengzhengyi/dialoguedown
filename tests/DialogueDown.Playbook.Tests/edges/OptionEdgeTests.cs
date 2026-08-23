using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Edges;

public sealed class OptionEdgeTests
{
    [Fact]
    public void RoundTrip_AGuardedOption_KeepsItsLabelAndCondition()
    {
        // The label is compiled in, so presenting a menu never peeks at the target node.
        const string Json = """
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
            """;

        PlaybookJsonAssert.AssertRoundTrip<Edge, OptionEdge>(Json);
    }

    [Fact]
    public void RoundTrip_AnUnguardedOption_OmitsTheCondition()
    {
        const string Json = """
            {
              "kind": "option",
              "target": 3,
              "label": [
                {
                  "kind": "text",
                  "text": "Say nothing"
                }
              ]
            }
            """;

        var option = PlaybookJsonAssert.AssertRoundTrip<Edge, OptionEdge>(Json);

        Assert.Null(option.Condition);
    }
}
