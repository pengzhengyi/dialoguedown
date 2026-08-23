using DialogueDown.Playbook.Tests.Support;
using DialogueDown.Playbook.Weights;
namespace DialogueDown.Playbook.Tests.Weights;

public sealed class AutoWeightTests
{
    [Fact]
    public void RoundTrip_AnAutomaticWeight_IsJustItsKind()
    {
        // An unweighted option shares whatever the weighted ones leave; it carries no number.
        const string Json = """
            {
              "kind": "auto"
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<ChoiceWeight, AutoWeight>(Json);
    }
}
