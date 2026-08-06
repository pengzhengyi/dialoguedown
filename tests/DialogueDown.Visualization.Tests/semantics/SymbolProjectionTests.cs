using DialogueDown.Configuration;
using DialogueDown.Visualization.Semantics;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Semantics;

public sealed class SymbolProjectionTests
{
    [Fact]
    public void Project_NullModel_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new SymbolProjection().Project(null!));

    [Fact]
    public void Project_JumpTargets_AreTheAnchoredScenes()
    {
        var symbols = Project(
            """
            # The Market

            Alice: Fresh apples!

            # The Dark Forest

            Bob: Spooky.
            """);

        Assert.Contains(symbols.JumpTargets, target => target is { Slug: "the-market", Heading: "The Market" });
        Assert.Contains(
            symbols.JumpTargets, target => target is { Slug: "the-dark-forest", Heading: "The Dark Forest" });
    }

    [Fact]
    public void Project_Speakers_ResolveNamesAndIds()
    {
        var symbols = Project("Guide @guide: Welcome.");

        Assert.Contains("Guide", symbols.Speakers);
        Assert.Contains("guide", symbols.SpeakerIds);
    }

    [Fact]
    public void Project_UnifiesASpeakerReferencedByIdThenNamed()
    {
        // The id is used before the declaration names it; the analyzer unifies them into one
        // symbol, so the name and the id both complete.
        var symbols = Project(
            """
            @guide: A voice speaks.

            Guide @guide: I am the guide.
            """);

        Assert.Equal(["Guide"], symbols.Speakers);
        Assert.Equal(["guide"], symbols.SpeakerIds);
    }

    [Fact]
    public void Project_Tags_AreTheSpeakersMergedTags()
    {
        var symbols = Project("Guide @guide #wise #calm: Welcome.");

        Assert.Contains("wise", symbols.Tags);
        Assert.Contains("calm", symbols.Tags);
    }

    [Fact]
    public void Project_ADocumentWithoutScenesOrNamedSpeakers_HasOnlyTheReservedTerminator()
    {
        var symbols = Project("The room is quiet.");

        // The reserved #END terminator is always offered; there are no scene targets here.
        Assert.Equal(["END"], symbols.JumpTargets.Select(target => target.Slug));
        Assert.Empty(symbols.Speakers);
        Assert.Empty(symbols.SpeakerIds);
    }

    [Fact]
    public void Project_JumpTargets_IncludeTheReservedEndTerminator()
    {
        var symbols = Project("Alice: Farewell.");

        Assert.Contains(symbols.JumpTargets, target => target is { Slug: "END", Heading: "End the run" });
        Assert.Equal(
            [new ReservedTargetSymbol("END", "End", ReservedTargetRole.Terminal)],
            symbols.ReservedTargets);
    }

    [Fact]
    public void Baseline_IncludesTheReservedEndTargetWithoutDocumentSymbols()
    {
        Assert.Equal(["END"], SymbolSet.Baseline.JumpTargets.Select(target => target.Slug));
        Assert.Equal(
            [new ReservedTargetSymbol("END", "End", ReservedTargetRole.Terminal)],
            SymbolSet.Baseline.ReservedTargets);
        Assert.Empty(SymbolSet.Baseline.Speakers);
        Assert.Empty(SymbolSet.Baseline.SpeakerIds);
        Assert.Empty(SymbolSet.Baseline.Tags);
    }

    [Fact]
    public void Project_ATagSharedByTwoSpeakers_AppearsOnce()
    {
        var symbols = Project(
            """
            Alice #main: Hi.

            Bob #main: Yo.
            """);

        Assert.Single(symbols.Tags, tag => tag == "main");
    }

    [Fact]
    public void Project_IncludesAConfiguredSpeakerTheScriptNeverUses()
    {
        // The whole point of config-aware completion: a speaker declared in dialogue.toml
        // completes in the editor even before a line uses it.
        var options = new CompilerOptions
        {
            Speakers = [new ConfiguredSpeaker("Narrator", null, [], [])],
        };

        var symbols = new SymbolProjection().Project(Pipeline.Model("The room is quiet.", options));

        Assert.Contains("Narrator", symbols.Speakers);
    }

    private static SymbolSet Project(string source) =>
        new SymbolProjection().Project(Pipeline.Model(source));
}
