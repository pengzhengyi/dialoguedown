using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

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
