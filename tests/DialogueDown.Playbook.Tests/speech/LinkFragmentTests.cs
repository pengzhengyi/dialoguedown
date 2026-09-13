using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speech;

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

    [Fact]
    public void Equality_EqualLabels_AreEqual()
    {
        var left = new LinkFragment("https://example.com", [new TextFragment("the notice")]);
        var right = new LinkFragment("https://example.com", [new TextFragment("the notice")]);

        Assert.True(left == right);
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentLabels_AreNotEqual()
    {
        var left = new LinkFragment("https://example.com", [new TextFragment("one")]);
        var right = new LinkFragment("https://example.com", [new TextFragment("another")]);

        Assert.False(left == right);
    }
}
