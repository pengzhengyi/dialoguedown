using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speech;

public sealed class TagFragmentTests
{
    [Fact]
    public void RoundTrip_AReservedTagWithAValue_KeepsBoth()
    {
        const string Json = """
            {
              "kind": "tag",
              "name": "mood",
              "value": "warm",
              "reserved": true
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, TagFragment>(Json);
    }

    [Fact]
    public void RoundTrip_ATagWithoutAValue_OmitsIt()
    {
        // The format never writes null: an absent value is absent from the document.
        const string Json = """
            {
              "kind": "tag",
              "name": "aside"
            }
            """;

        var tag = PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, TagFragment>(Json);

        Assert.Null(tag.Value);
    }

    [Fact]
    public void Construct_WithoutAName_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new TagFragment(null!, Value: null, Reserved: false));
    }
}
