using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class MermaidFenceTests
{
    [Theory]
    [InlineData("```mermaid\nflowchart LR\n```")]
    [InlineData("~~~mermaid\nflowchart LR\n~~~")]
    [InlineData("Prose.\n\n   ```MERMAID\nflowchart LR\n```")]
    [InlineData("````mermaid  \nflowchart LR\n````")]
    [InlineData("```mermaid darkMode\nflowchart LR\n```")]
    public void AppearsIn_FindsAFenceThatWouldRenderADiagram(string source)
    {
        Assert.True(MermaidFence.AppearsIn(source));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Alice: mermaid is a lovely word.")]
    [InlineData("```csharp\nvar mermaid = 1;\n```")]
    [InlineData("```mermaidish\nnot a diagram\n```")]
    [InlineData("`mermaid`")]
    public void AppearsIn_IgnoresProseAndOtherLanguages(string? source)
    {
        Assert.False(MermaidFence.AppearsIn(source));
    }

    [Fact]
    public void AppearsIn_ErrsTowardYesForAFenceItCannotFullyJudge()
    {
        // Guessing "no" would export a script whose diagram cannot draw; guessing "yes" only
        // makes the file bigger. A fence nested inside another one takes the harmless answer.
        Assert.True(MermaidFence.AppearsIn("````markdown\n```mermaid\nflowchart LR\n```\n````"));
    }
}
