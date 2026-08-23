using DialogueDown.Playbook.Nodes;
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
}
