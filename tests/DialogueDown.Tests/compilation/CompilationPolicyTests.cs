using DialogueDown.Compilation;
using DialogueDown.Configuration;

namespace DialogueDown.Tests.Compilation;

public sealed class CompilationPolicyTests
{
    [Fact]
    public void For_TakesTheModeAndDisabledCodesFromTheOptions()
    {
        var options = CompilerOptions.Default with
        {
            Mode = CompilationMode.BestEffort,
            DisabledDiagnostics = ["DLG3002"],
        };

        var policy = CompilationPolicy.For(options);

        Assert.Equal(CompilationMode.BestEffort, policy.Mode);
        Assert.Equal(["DLG3002"], policy.DisabledDiagnostics);
    }

    [Fact]
    public void For_NullOptions_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CompilationPolicy.For(null!));

    [Fact]
    public void Of_DisablesNothing()
    {
        var policy = CompilationPolicy.Of(CompilationMode.FailFast);

        Assert.Equal(CompilationMode.FailFast, policy.Mode);
        Assert.Empty(policy.DisabledDiagnostics);
    }
}
