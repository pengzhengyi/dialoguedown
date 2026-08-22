using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speech;

public sealed class TextFragmentTests
{
    [Fact]
    public void RoundTrip_ThroughTheUnion_KeepsTheKind()
    {
        const string Json = """
            {
              "kind": "text",
              "text": "My favorite color is "
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, TextFragment>(Json);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Construct_WithoutContent_IsRejected(string? text)
    {
        // Plain text with nothing in it is never produced, and a reader that accepted it
        // would emit an invisible line.
        Assert.ThrowsAny<ArgumentException>(() => new TextFragment(text!));
    }
}
