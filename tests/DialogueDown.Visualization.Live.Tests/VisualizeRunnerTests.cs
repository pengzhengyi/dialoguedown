using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class VisualizeRunnerTests
{
    [Fact]
    public void RunStatic_WritesReportAndOpensIt()
    {
        using var doc = new TempDocument("# Scene");
        var browser = new FakeBrowserLauncher();
        var runner = new VisualizeRunner(browser);

        var code = runner.RunStatic(doc.Path, output: null, noOpen: false, AppliedConfiguration.WithoutFile(CompilerOptions.Default));

        Assert.Equal(0, code);
        var opened = Assert.Single(browser.Opened);
        Assert.EndsWith(".html", opened);
        Assert.True(File.Exists(opened));
        File.Delete(opened);
    }

    [Fact]
    public void RunStatic_Output_WritesToThePathWithoutOpening()
    {
        using var doc = new TempDocument("# Scene");
        var target = Path.Combine(Path.GetTempPath(), $"dd-vr-{Guid.NewGuid():N}.html");
        var browser = new FakeBrowserLauncher();
        var runner = new VisualizeRunner(browser);

        try
        {
            var code = runner.RunStatic(doc.Path, target, noOpen: true, AppliedConfiguration.WithoutFile(CompilerOptions.Default));

            Assert.Equal(0, code);
            Assert.True(File.Exists(target));
            Assert.Empty(browser.Opened);
        }
        finally
        {
            File.Delete(target);
        }
    }
}
