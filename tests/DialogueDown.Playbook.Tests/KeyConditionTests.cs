using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class KeyConditionTests
{
    [Fact]
    public void RoundTrip_AKeyCondition_KeepsItsKey()
    {
        const string Json = """
            {
              "kind": "key",
              "key": "IsCurious"
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<Condition, KeyCondition>(Json);
    }

    [Fact]
    public void Construct_WithoutAKey_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyCondition(null!));
    }
}
