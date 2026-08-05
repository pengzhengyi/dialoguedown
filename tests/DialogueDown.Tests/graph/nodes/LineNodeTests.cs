using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Graph.Nodes;

public sealed class LineNodeTests
{
    [Fact]
    public void Effects_AreTheGameCallsInSpeech_InOrder()
    {
        var give = new CustomCommand("GiveGold", ["5"], SourceSpanFactory.Span());
        var mood = new Query("Player.Mood", SourceSpanFactory.Span());

        var node = Node(Text("You get "), give, Text(", feeling "), mood);

        Assert.Equal<GameCall>([give, mood], node.Effects);
    }

    [Fact]
    public void Effects_AreEmpty_WhenSpeechHasNoGameCalls()
    {
        var node = Node(Text("Just words."));

        Assert.Empty(node.Effects);
    }

    private static LineNode Node(params InlineFragment[] speech) =>
        new(NodeId(0), SpeakerSymbol.ForName("Alice"), speech, []);

    private static Text Text(string content) => DialogueAstFactory.Text(content);
}
