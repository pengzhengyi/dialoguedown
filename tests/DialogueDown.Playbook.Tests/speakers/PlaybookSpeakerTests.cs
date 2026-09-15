using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speakers;

public sealed class PlaybookSpeakerTests
{
    [Fact]
    public void RoundTrip_ANamedSpeaker_KeepsItsTags()
    {
        const string Json = """
            {
              "id": "alice",
              "name": "Alice",
              "tags": [
                {
                  "name": "mood",
                  "value": "warm"
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<PlaybookSpeaker>(Json);
    }

    [Fact]
    public void RoundTrip_TheAnonymousDefault_HasNoName()
    {
        // A script with no speaker declared still says lines; they belong to a default nobody named.
        const string Json = """
            {
              "id": "speaker-1",
              "default": true,
              "tags": []
            }
            """;

        var speaker = PlaybookJsonAssert.AssertRoundTrip<PlaybookSpeaker>(Json);

        Assert.Null(speaker.Name);
        Assert.True(speaker.Default);
    }

    [Fact]
    public void Construct_WithNeitherNameNorId_IsAllowed()
    {
        // The anonymous default has neither. A line reaches it by index, so nothing about a
        // speaker has to be invented to make them addressable.
        var anonymous = new PlaybookSpeaker(Id: null, Name: null, Default: true, Tags: []);

        Assert.Null(anonymous.Id);
        Assert.Null(anonymous.Name);
    }

    [Fact]
    public void Equality_EqualTags_AreEqual()
    {
        var left = new PlaybookSpeaker("alice", "Alice", false, [new SpeakerTag("mood", "warm", false)]);
        var right = new PlaybookSpeaker("alice", "Alice", false, [new SpeakerTag("mood", "warm", false)]);

        EqualityAssert.AssertValueEqual(left, right);
    }

    [Fact]
    public void Equality_DifferentTags_AreNotEqual()
    {
        var left = new PlaybookSpeaker("alice", "Alice", false, [new SpeakerTag("mood", "warm", false)]);
        var right = new PlaybookSpeaker("alice", "Alice", false, [new SpeakerTag("mood", "cold", false)]);

        EqualityAssert.AssertValueUnequal(left, right);
    }
}
