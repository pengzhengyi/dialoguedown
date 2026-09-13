using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Nodes;

public sealed class LineNodeTests
{
    [Fact]
    public void RoundTrip_ASpokenLine_KeepsSpeakerAndSpeech()
    {
        const string Json = """
            {
              "kind": "line",
              "id": 0,
              "speaker": 0,
              "speech": [
                {
                  "kind": "text",
                  "text": "My favorite color is "
                },
                {
                  "kind": "query",
                  "key": "Alice.FavoriteColor"
                }
              ],
              "out": [
                {
                  "kind": "succession",
                  "target": 1
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Node, LineNode>(Json);
    }

    [Fact]
    public void Construct_WithAnImpossibleSpeaker_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LineNode(0, -1, [], Condition: null, []));
    }

    [Fact]
    public void Equality_EqualSpeech_AreEqual()
    {
        var left = new LineNode(0, 0, [new TextFragment("hello")], Condition: null, [new SuccessionEdge(1)]);
        var right = new LineNode(0, 0, [new TextFragment("hello")], Condition: null, [new SuccessionEdge(1)]);

        Assert.True(left == right);
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentSpeech_AreNotEqual()
    {
        var left = new LineNode(0, 0, [new TextFragment("hello")], Condition: null, []);
        var right = new LineNode(0, 0, [new TextFragment("goodbye")], Condition: null, []);

        Assert.False(left == right);
    }
}
