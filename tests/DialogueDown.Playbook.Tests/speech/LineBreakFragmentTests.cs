using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speech;

public sealed class LineBreakFragmentTests
{
    [Fact]
    public void RoundTrip_ABreak_IsJustItsKind()
    {
        // A break carries nothing; the kind is the whole fragment.
        const string Json = """
            {
              "kind": "break"
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, LineBreakFragment>(Json);
    }
}
