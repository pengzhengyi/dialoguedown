using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class ImageFragmentTests
{
    [Fact]
    public void RoundTrip_ImageWithAlternativeText_PreservesBoth()
    {
        const string Json = """
            {
              "kind": "image",
              "source": "portrait.png",
              "alt": [
                {
                  "kind": "text",
                  "text": "Alice smiling"
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, ImageFragment>(Json);
    }

    [Fact]
    public void RoundTrip_ImageWithoutAlternativeText_KeepsAnEmptyList()
    {
        // Alternative text is optional in the source, so an empty list is a valid document
        // rather than a malformed one.
        const string Json = """
            {
              "kind": "image",
              "source": "portrait.png",
              "alt": []
            }
            """;

        var image = PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, ImageFragment>(Json);

        Assert.Empty(image.Alt);
    }

    [Fact]
    public void Construct_WithoutASource_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ImageFragment(null!, []));
    }
}
