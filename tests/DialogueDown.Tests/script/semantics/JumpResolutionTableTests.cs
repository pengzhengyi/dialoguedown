using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class JumpResolutionTableTests
{
    [Fact]
    public void Resolve_ReturnsTheResolutionForAKnownJump()
    {
        var jump = Jump("#play");
        var resolution = new UnresolvedJump();
        var table = new JumpResolutionTable(new Dictionary<Jump, JumpResolution> { [jump] = resolution });

        Assert.Same(resolution, table.Resolve(jump));
    }

    [Fact]
    public void Resolve_UnanalyzedJump_Throws()
    {
        var table = new JumpResolutionTable(new Dictionary<Jump, JumpResolution>());

        Assert.Throws<ArgumentException>(() => table.Resolve(Jump("#play")));
    }

    [Fact]
    public void Resolve_AnEqualCopyOfAnAnalyzedJump_Throws()
    {
        var jump = Jump("#play");
        var table = new JumpResolutionTable(new Dictionary<Jump, JumpResolution> { [jump] = new UnresolvedJump() });

        // The copy holds the same values, but it is not the jump that was analyzed.
        Assert.Throws<ArgumentException>(() => table.Resolve(jump with { }));
    }

    [Fact]
    public void Resolutions_AndCount_ReflectTheEntries()
    {
        var table = new JumpResolutionTable(new Dictionary<Jump, JumpResolution>
        {
            [Jump("#a")] = new UnresolvedJump(),
            [Jump("#b")] = new FileScopedJump("x.md", null),
        });

        Assert.Equal(2, table.Count);
        Assert.Collection(
            table.Resolutions,
            resolution => Assert.IsType<UnresolvedJump>(resolution),
            resolution => Assert.IsType<FileScopedJump>(resolution));
    }
}
