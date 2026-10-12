using DialogueDown.Common;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class TagExtensionsTests
{
    [Fact]
    public void SemanticKey_IsNameAndValue()
    {
        var tag = CustomTag("mood", "happy");

        Assert.Equal(("mood", "happy"), tag.SemanticKey());
    }

    [Fact]
    public void SemanticKey_IgnoresTheSpan()
    {
        var here = CustomTag("mood", "happy");
        var elsewhere = here with { Span = new SourceSpan(99, 5) };

        Assert.Equal(here.SemanticKey(), elsewhere.SemanticKey());
    }

    [Fact]
    public void SemanticKey_TreatsADifferentValueAsDistinct()
    {
        var happy = CustomTag("mood", "happy");
        var sad = CustomTag("mood", "sad");

        Assert.NotEqual(happy.SemanticKey(), sad.SemanticKey());
    }

    [Fact]
    public void SemanticKey_HandlesANullValue()
    {
        var tag = CustomTag("happy");

        Assert.Equal(("happy", (string?)null), tag.SemanticKey());
    }
}
