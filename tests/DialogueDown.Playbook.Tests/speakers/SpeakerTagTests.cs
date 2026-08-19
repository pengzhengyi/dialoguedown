using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class SpeakerTagTests
{
    [Fact]
    public void RoundTrip_ATagWithAValue_KeepsBoth()
    {
        const string Json = """
            {
              "name": "mood",
              "value": "warm",
              "reserved": true
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeakerTag>(Json);
    }

    [Fact]
    public void Construct_WithoutAName_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new SpeakerTag(null!, Value: null, Reserved: false));
    }
}
