using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class LinkFragmentTests
{
    [Fact]
    public void RoundTrip_LinkWithStyledLabel_PreservesTheLabel()
    {
        // A label is speech in its own right, so it nests like any other fragment list.
        const string Json = """
            {
              "kind": "link",
              "target": "https://example.com",
              "label": [
                {
                  "kind": "text",
                  "text": "the notice"
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, LinkFragment>(Json);
    }

    [Fact]
    public void Construct_WithoutATarget_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new LinkFragment(null!, []));
    }
}
