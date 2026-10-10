using DialogueDown.Configuration;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class EmitModeTests
{
    [Fact]
    public void Run_Dot_WithOutput_WritesDigraphToTheFileNotStdout()
    {
        using var tree = new TempTree();
        var script = tree.File("scene.dialogue.md", """
            # Scene

            Alice: Hi.
            """);
        var output = Path.Combine(tree.Root, "graph.dot");
        var stdout = new StringWriter();

        var code = EmitMode.Run(script, EmitFormat.Dot, output, CompilerOptions.Default, stdout, new StringWriter());

        Assert.Equal(0, code);
        Assert.True(File.Exists(output));
        Assert.Contains("digraph", File.ReadAllText(output));
        Assert.Equal(string.Empty, stdout.ToString());
    }

    [Fact]
    public void Run_BadDocument_WritesProblemToErrorAndReturnsNonZero()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"dd-missing-{Guid.NewGuid():N}.dialogue.md");
        var stdout = new StringWriter();
        var error = new StringWriter();

        var code = EmitMode.Run(missing, EmitFormat.Dot, output: null, CompilerOptions.Default, stdout, error);

        Assert.NotEqual(0, code);
        Assert.NotEqual(string.Empty, error.ToString());
        Assert.Equal(string.Empty, stdout.ToString());
    }
}
