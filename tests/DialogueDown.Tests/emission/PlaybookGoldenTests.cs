using DialogueDown.Playbook.Nodes;
using DialogueDown.Tests.Support;
using DialogueDown.TestSupport;

namespace DialogueDown.Tests.Emission;

/// <summary>
/// The playbook each shipped example compiles to, committed so a change to the format is a
/// reviewable diff rather than something noticed later.
/// </summary>
/// <remarks>
/// Regenerate by accepting the .received.json files Verify writes beside the goldens. They churn
/// whenever node positions shift, which is expected: a playbook is a build artifact nobody edits.
/// </remarks>
public sealed class PlaybookGoldenTests
{
    // The one example with no playbook: it exists to show what diagnostics look like, so it does
    // not compile. Named rather than quietly skipped, so making it compile fails the test below
    // and asks for a golden instead of passing in silence.
    private const string ShowsDiagnostics = "diagnostics.dialogue.md";

    public static TheoryData<string> Examples() =>
        [.. ExampleScripts.Names().Where(name => name != ShowsDiagnostics)];

    [Fact]
    public void TheOnlyExampleWithoutAPlaybook_IsTheOneThatShowsDiagnostics()
    {
        CompilationAssert.AssertFailure(Pipeline.Compile(ExampleScripts.Read(ShowsDiagnostics)));
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public Task Compiling_AnExample_WritesThePlaybookItAlwaysDid(string example)
    {
        var playbook = Playbooks.Of(ExampleScripts.Read(example), example);

        return Verify(Playbooks.Serialize(playbook), extension: "json")
            .UseDirectory("goldens")
            .UseFileName(example.Replace(".dialogue.md", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public void VisualNovel_TheLinesThatOpenWithAGameCall_BindToYuki()
    {
        var playbook = Playbooks.Of(
            ExampleScripts.Read("visual-novel.dialogue.md"), "visual-novel.dialogue.md");

        var yuki = playbook.Speakers[1];
        Assert.Equal("yuki", yuki.Id);
        Assert.Equal("Yuki", yuki.Name);
        Assert.Equal(["heroine"], yuki.Tags.Select(tag => tag.Name));

        int[] lines = [2, 7, 11, 23, 27, 35];
        foreach (var id in lines)
        {
            var line = Assert.IsType<LineNode>(
                Assert.Single(playbook.Nodes, node => node.Id == id));
            Assert.Equal(1, line.Speaker);
        }
    }
}
