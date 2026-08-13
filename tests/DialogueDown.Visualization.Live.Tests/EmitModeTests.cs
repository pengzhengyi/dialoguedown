using DialogueDown.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class EmitModeTests
{
    [Fact]
    public void Run_Dot_WithOutput_WritesDigraphToTheFileNotStdout()
    {
        using var doc = new TempDocument("# Scene\n\nAlice: Hi.");
        var writer = new StringWriter();
        var output = Path.Combine(Path.GetTempPath(), $"dd-emit-{Guid.NewGuid():N}.dot");

        try
        {
            var code = EmitMode.Run(doc.Path, EmitFormat.Dot, output, CompilerOptions.Default, writer, new StringWriter());

            Assert.Equal(0, code);
            Assert.True(File.Exists(output));
            Assert.Contains("digraph", File.ReadAllText(output));
            Assert.Equal(string.Empty, writer.ToString()); // went to the file, not stdout
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Fact]
    public void Run_BadDocument_WritesProblemToErrorAndReturnsNonZero()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"dd-missing-{Guid.NewGuid():N}.dialogue.md");
        var writer = new StringWriter();
        var error = new StringWriter();

        var code = EmitMode.Run(missing, EmitFormat.Dot, output: null, CompilerOptions.Default, writer, error);

        Assert.NotEqual(0, code);
        Assert.NotEqual(string.Empty, error.ToString());
        Assert.Equal(string.Empty, writer.ToString()); // nothing emitted
    }
}
