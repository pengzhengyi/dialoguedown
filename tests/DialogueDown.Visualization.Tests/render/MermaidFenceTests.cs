using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class MermaidFenceTests
{
    [Theory]
    [InlineData("""
        ```mermaid
        flowchart LR
        ```
        """)]
    [InlineData("""
        ~~~mermaid
        flowchart LR
        ~~~
        """)]
    [InlineData("""
        Prose.

           ```MERMAID
        flowchart LR
        ```
        """)]
    // Spaces after the language name, written escaped so an editor cannot trim them away.
    [InlineData("````mermaid  \nflowchart LR\n````")]
    [InlineData("""
        ```mermaid darkMode
        flowchart LR
        ```
        """)]
    public void AppearsIn_FindsAFenceThatWouldRenderADiagram(string source)
    {
        Assert.True(MermaidFence.AppearsIn(source));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Alice: mermaid is a lovely word.")]
    [InlineData("""
        ```csharp
        var mermaid = 1;
        ```
        """)]
    [InlineData("""
        ```mermaidish
        not a diagram
        ```
        """)]
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
        const string FenceInsideAFence = """
            ````markdown
            ```mermaid
            flowchart LR
            ```
            ````
            """;

        Assert.True(MermaidFence.AppearsIn(FenceInsideAFence));
    }
}
