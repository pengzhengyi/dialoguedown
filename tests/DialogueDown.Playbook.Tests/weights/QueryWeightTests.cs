using DialogueDown.Playbook.Tests.Support;
using DialogueDown.Playbook.Weights;
namespace DialogueDown.Playbook.Tests.Weights;

public sealed class QueryWeightTests
{
    [Fact]
    public void RoundTrip_AQueriedWeight_KeepsItsKey()
    {
        // The host answers with a number at play time; the playbook only carries the question.
        const string Json = """
            {
              "kind": "query",
              "key": "Bob.Affection"
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<ChoiceWeight, QueryWeight>(Json);
    }

    [Fact]
    public void Construct_WithoutAKey_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new QueryWeight(null!));
    }
}
