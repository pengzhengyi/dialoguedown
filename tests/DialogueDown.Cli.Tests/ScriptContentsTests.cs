using System.Text;
using DialogueDown.TestSupport;

namespace DialogueDown.Cli.Tests;

public sealed class ScriptContentsTests
{
    [Fact]
    public void Read_BomlessText_KeepsTheTextAndReportsNoBom()
    {
        using var tree = new TempTree();
        var path = tree.File("script.dialogue.md", "Alice: Hi.");

        var contents = ScriptContents.Read(path);

        Assert.Equal("Alice: Hi.", contents.Text);
        Assert.False(contents.HasBom);
    }

    [Fact]
    public void Read_TextWithABom_StripsItAndRemembersIt()
    {
        using var tree = new TempTree();
        var path = tree.File("script.dialogue.md");
        File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("Alice: Hi.")]);

        var contents = ScriptContents.Read(path);

        Assert.Equal("Alice: Hi.", contents.Text);
        Assert.True(contents.HasBom);
    }

    [Fact]
    public void Write_WhenTheFileHadABom_RestoresIt()
    {
        using var tree = new TempTree();
        var path = tree.File("script.dialogue.md");

        new ScriptContents("Alice: Hi.", HasBom: true).Write(path);

        Assert.Equal([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("Alice: Hi.")], File.ReadAllBytes(path));
    }

    [Fact]
    public void Write_WhenTheFileHadNoBom_WritesNoPreamble()
    {
        using var tree = new TempTree();
        var path = tree.File("script.dialogue.md");

        new ScriptContents("Alice: Hi.", HasBom: false).Write(path);

        Assert.Equal(Encoding.UTF8.GetBytes("Alice: Hi."), File.ReadAllBytes(path));
    }
}
