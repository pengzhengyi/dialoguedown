using System.Text.Json;
using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Graph;
using DialogueDown.Graph.Regions;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Checking;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using GraphEndNode = DialogueDown.Graph.Nodes.EndNode;

namespace DialogueDown.Tests.Emission;

public sealed class PlaybookWriterTests
{
    private const string Script = "chapter-01.dialogue.md";

    private readonly PlaybookWriter _writer = new();

    [Fact]
    public void Write_AGraph_SaysWhichScriptItWasCompiledFrom()
    {
        var playbook = _writer.Write(OneEndNode(), Script);

        Assert.Equal(Script, playbook.Script);
    }

    [Fact]
    public void Write_AGraph_StatesTheFormatThisBuildWrites()
    {
        var playbook = _writer.Write(OneEndNode(), Script);

        Assert.Equal(PlaybookWriter.FormatVersion, playbook.Format.Version);
        Assert.Contains(Capabilities.Core, playbook.Format.Requires);
    }

    [Fact]
    public void Write_TheFormatThisBuildWrites_IsOneThisBuildReads()
    {
        // The writer states its own version instead of reading the reader's range, so that the
        // two agree is proven here rather than assumed by sharing a constant.
        var playbook = _writer.Write(OneEndNode(), Script);

        PlaybookCheckerFactory.CreateFormat().Check(playbook);
    }

    [Fact]
    public void Write_AGraph_NumbersEveryNodeByWhereItSits()
    {
        var playbook = _writer.Write(Graph([EndNode(12), EndNode(3)], entry: 12), Script);

        Assert.Equal([0, 1], playbook.Nodes.Select(node => node.Id));
    }

    [Fact]
    public void Write_AnEntryThatIsNotTheFirstNode_PointsAtItsPosition()
    {
        var playbook = _writer.Write(Graph([EndNode(12), EndNode(3)], entry: 3), Script);

        Assert.Equal(1, playbook.Entry);
    }

    [Fact]
    public void Write_AGraph_ProducesAPlaybookAReaderAcceptsUnchanged()
    {
        // Compared as text, because the format is the bytes: a document that reads back to the
        // same JSON lost nothing on the way through.
        var written = Serialize(_writer.Write(Graph([EndNode(12), EndNode(3)], entry: 3), Script));

        var read = PlaybookReader.Default.Read(written);

        Assert.Equal(written, Serialize(read));
    }

    [Fact]
    public void Write_AScriptUsingEverything_SurvivesBeingReadBack()
    {
        // The whole point of the format in one test: whatever the compiler can produce, a reader
        // takes back unchanged.
        var compilation = CompilationAssert.AssertSuccess(
            ScriptCompilerFactory.CreateDefault().Compile("""
                # Gate

                Alice @A #main: Who goes there? `Fanfare()`

                - Bob: A friend. => [the inn](#the-inn)

                - Bob: Nobody.

                # The Inn

                Innkeeper: Welcome.
                """));

        var written = Serialize(_writer.Write(compilation, Script));

        Assert.Equal(written, Serialize(PlaybookReader.Default.Read(written)));
    }

    [Fact]
    public void Write_AScriptWithSpeakers_ListsThemOnceEach()
    {
        var compilation = CompilationAssert.AssertSuccess(
            ScriptCompilerFactory.CreateDefault().Compile("""
                Alice: Hello.

                Bob: Hello yourself.

                Alice: Goodbye.
                """));

        var playbook = _writer.Write(compilation, Script);

        Assert.Equal(["Alice", "Bob"], playbook.Speakers.Select(speaker => speaker.Name));
    }

    [Fact]
    public void Write_AScriptWithNoDialogue_WritesAPlaybookThatEndsAtOnce()
    {
        var compilation = CompilationAssert.AssertSuccess(
            ScriptCompilerFactory.CreateDefault().Compile(string.Empty));

        var playbook = _writer.Write(compilation, Script);

        Assert.Equal(playbook.Entry, Assert.Single(playbook.Nodes).Id);
    }

    [Fact]
    public void Write_NoCompilationAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _writer.Write((CompilationSuccess)null!, Script));
    }

    [Fact]
    public void Write_NoScriptName_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _writer.Write(OneEndNode(), null!));
    }

    private static string Serialize(PlaybookDocument playbook) =>
        JsonSerializer.Serialize(playbook, PlaybookJson.Options);

    private static DialogueGraph OneEndNode() => Graph([EndNode(0)], entry: 0);

    private static DialogueGraph Graph(GraphEndNode[] nodes, int entry) =>
        new(nodes, NodeId(entry), nodes[^1].Id, RegionTree.Empty);

}
