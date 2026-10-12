using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SpeakerTableTests
{
    [Fact]
    public void Resolve_Declaration_ReturnsItsSymbol()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(alice, table.Resolve(SpeakerDeclaration("Alice", "A")));
    }

    [Fact]
    public void Resolve_NameReference_ReturnsTheSymbolByName()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(alice, table.Resolve(SpeakerNameReference("Alice")));
    }

    [Fact]
    public void Resolve_IdReference_ReturnsTheSymbolById()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(alice, table.Resolve(SpeakerIdReference("A")));
    }

    [Fact]
    public void Resolve_PartialDeclaration_ReturnsTheSymbolById()
    {
        var alice = Alice();
        var table = Table(alice);

        Assert.Same(
            alice,
            table.Resolve(PartialSpeakerDeclaration("A")));
    }

    [Fact]
    public void Resolve_DefaultSpeaker_ReturnsTheProvidedDefault()
    {
        var fallback = SpeakerSymbol.ForName("Narrator");
        var table = new SpeakerTable(
            new Dictionary<string, SpeakerSymbol>(),
            new Dictionary<string, SpeakerSymbol>(),
            fallback);

        Assert.Same(fallback, table.Resolve(DefaultSpeaker()));
    }

    [Fact]
    public void Resolve_UnknownSpeakerKind_Throws()
    {
        var table = Table(Alice());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => table.Resolve(new UnknownSpeaker(SourceSpanFactory.Span())));
    }

    [Fact]
    public void Symbols_CountsASpeakerKeyedByNameAndIdOnce()
    {
        var alice = Alice();

        Assert.Same(alice, Assert.Single(Table(alice).Symbols));
    }

    [Fact]
    public void Symbols_IncludeNameOnlyAndIdOnlySpeakers()
    {
        var narrator = SpeakerSymbol.ForName("Narrator");
        var ghost = SpeakerSymbol.ForId("G");

        var symbols = Table(Alice(), narrator, ghost).Symbols;

        Assert.Equal(3, symbols.Count);
        Assert.Contains(narrator, symbols);
        Assert.Contains(ghost, symbols);
    }

    [Fact]
    public void Symbols_ExcludeTheAnonymousDefault()
    {
        var table = new SpeakerTable(
            new Dictionary<string, SpeakerSymbol>(),
            new Dictionary<string, SpeakerSymbol>(),
            SpeakerSymbol.ForName("Narrator"));

        Assert.Empty(table.Symbols);
    }

    private static SpeakerSymbol Alice()
    {
        var alice = SpeakerSymbol.ForName("Alice");
        alice.GiveId("A");
        return alice;
    }

    private static SpeakerTable Table(params SpeakerSymbol[] symbols)
    {
        var byName = new Dictionary<string, SpeakerSymbol>();
        var byId = new Dictionary<string, SpeakerSymbol>();
        foreach (var symbol in symbols)
        {
            if (symbol.Name is { } name)
            {
                byName.Add(name, symbol);
            }

            if (symbol.Id is { } id)
            {
                byId.Add(id, symbol);
            }
        }

        return new SpeakerTable(byName, byId, SpeakerSymbol.ForName("Default"));
    }

    private sealed record UnknownSpeaker(SourceSpan Span) : Speaker(Span);
}
