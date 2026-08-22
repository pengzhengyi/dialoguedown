using DialogueDown.Playbook.Tests.Support;
using DialogueDown.Playbook.Weights;
namespace DialogueDown.Playbook.Tests.Weights;

public sealed class NumberWeightTests
{
    [Fact]
    public void RoundTrip_AFixedWeight_KeepsItsPercentage()
    {
        const string Json = """
            {
              "kind": "number",
              "percentage": 25.5
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<ChoiceWeight, NumberWeight>(Json);
    }
}
