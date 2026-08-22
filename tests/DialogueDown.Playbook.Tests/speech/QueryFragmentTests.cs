using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Speech;

public sealed class QueryFragmentTests
{
    [Fact]
    public void RoundTrip_AQuery_KeepsItsKey()
    {
        const string Json = """
            {
              "kind": "query",
              "key": "Alice.FavoriteColor"
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, QueryFragment>(Json);
    }

    [Fact]
    public void Construct_WithoutAKey_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new QueryFragment(null!));
    }
}
