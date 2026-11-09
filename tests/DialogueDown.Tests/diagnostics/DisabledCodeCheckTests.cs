using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DisabledCodeCheckTests
{
    [Fact]
    public void IsValid_WithoutAProblem_IsTrue() =>
        Assert.True(new DisabledCodeCheck("DLG3002", Problem: null).IsValid);

    [Fact]
    public void IsValid_WithAProblem_IsFalse() =>
        Assert.False(new DisabledCodeCheck("DLG2001", "DLG2001 is an error and cannot be disabled.").IsValid);
}
