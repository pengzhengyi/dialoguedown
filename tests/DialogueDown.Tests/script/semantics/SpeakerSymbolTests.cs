using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.SpeakerSymbolAssert;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SpeakerSymbolTests
{
    [Fact]
    public void ForName_KnowsOnlyTheName()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        Assert.Equal("Alice", symbol.Name);
        Assert.Null(symbol.Id);
    }

    [Fact]
    public void ForId_KnowsOnlyTheId()
    {
        var symbol = SpeakerSymbol.ForId("A");

        Assert.Equal("A", symbol.Id);
        Assert.Null(symbol.Name);
    }

    [Fact]
    public void Anonymous_HasNoNameOrId()
    {
        var symbol = SpeakerSymbol.Anonymous();

        Assert.Null(symbol.Name);
        Assert.Null(symbol.Id);
    }

    [Fact]
    public void NewSymbol_HasNoTagsAndIsNotDefault()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        Assert.Empty(symbol.Tags);
        Assert.False(symbol.IsDefault);
    }

    [Fact]
    public void ToString_NamedSpeaker_IsTheName() =>
        Assert.Equal("Alice", SpeakerSymbol.ForName("Alice").ToString());

    [Fact]
    public void ToString_IdOnlySpeaker_IsTheAtId() =>
        Assert.Equal("@A", SpeakerSymbol.ForId("A").ToString());

    [Fact]
    public void ToString_AnonymousSpeaker_IsDefaultLabel() =>
        Assert.Equal("(default)", SpeakerSymbol.Anonymous().ToString());

    [Fact]
    public void GiveName_NamesAnIdOnlySymbol()
    {
        var symbol = SpeakerSymbol.ForId("A");

        symbol.GiveName("Alice");

        Assert.Equal("Alice", symbol.Name);
        Assert.Equal("A", symbol.Id);
    }

    [Fact]
    public void GiveId_IdsANameOnlySymbol()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.GiveId("A");

        Assert.Equal("A", symbol.Id);
        Assert.Equal("Alice", symbol.Name);
    }

    [Fact]
    public void MarkDefault_MakesItTheDefault()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MarkDefault();

        Assert.True(symbol.IsDefault);
    }

    [Fact]
    public void MergeTag_AddsASingleTag()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTag(TagAt("happy", null, 0));

        AssertTags(symbol, ("happy", null));
    }

    [Fact]
    public void MergeTag_SkipsATagWithTheSameIdentity()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTag(TagAt("happy", null, 0));
        symbol.MergeTag(TagAt("happy", null, 5));

        AssertTags(symbol, ("happy", null));
    }

    [Fact]
    public void MergeTags_AddsTheGivenTags()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("happy", null, 0), TagAt("mood", "calm", 1)]);

        AssertTags(symbol, ("happy", null), ("mood", "calm"));
    }

    [Fact]
    public void MergeTags_SkipsATagWithTheSameIdentity()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("happy", null, 0)]);
        symbol.MergeTags([TagAt("happy", null, 5), TagAt("excited", null, 1)]);

        AssertTags(symbol, ("happy", null), ("excited", null));
    }

    [Fact]
    public void MergeTags_KeepsSameNameWithADifferentValue()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("mood", "happy", 0), TagAt("mood", "sad", 1)]);

        AssertTags(symbol, ("mood", "happy"), ("mood", "sad"));
    }

    [Fact]
    public void Tags_AreOrderedBySpanStart_NotByInsertionOrder()
    {
        var symbol = SpeakerSymbol.ForName("Alice");

        symbol.MergeTags([TagAt("second", null, 10), TagAt("first", null, 0)]);

        AssertTags(symbol, ("first", null), ("second", null));
    }

    private static CustomTag TagAt(string name, string? value, int start) =>
        CustomTag(name, value) with { Span = new SourceSpan(start, 1) };
}
