using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class LineNodeTests
{
    [Fact]
    public void RoundTrip_ASpokenLine_KeepsSpeakerAndSpeech()
    {
        const string Json = """
            {
              "kind": "line",
              "id": 0,
              "speaker": "alice",
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
    public void Construct_WithoutASpeaker_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new LineNode(0, null!, [], Condition: null, []));
    }
}
