using DialogueDown.Configuration;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class VisualizeRunnerTests
{
    [Fact]
    public void RunStatic_WritesReportAndOpensIt()
    {
        using var script = new TempScript("# Scene");
        var browser = new FakeBrowserLauncher();
        var runner = new VisualizeRunner(browser);

        var code = runner.RunStatic(script.Path, output: null, noOpen: false, AppliedConfiguration.WithoutFile(CompilerOptions.Default));

        var opened = Assert.Single(browser.Opened);
        try
        {
            Assert.Equal(0, code);
            Assert.EndsWith(".html", opened);
            Assert.True(File.Exists(opened));
        }
        finally
        {
            File.Delete(opened);
        }
    }

    [Fact]
    public void RunStatic_Output_WritesToThePathWithoutOpening()
    {
        using var tree = new TempTree();
        var script = tree.File("scene.dialogue.md", "# Scene");
        var target = Path.Combine(tree.Root, "report.html");
        var browser = new FakeBrowserLauncher();
        var runner = new VisualizeRunner(browser);

        var code = runner.RunStatic(script, target, noOpen: true, AppliedConfiguration.WithoutFile(CompilerOptions.Default));

        Assert.Equal(0, code);
        Assert.True(File.Exists(target));
        Assert.Empty(browser.Opened);
    }
}
