using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Conformance;

public sealed class ReadableCorpusTests
{
    private const string Fixture = """
        {
          "name": "a case",
          "playbook": "playbook.json",
          "verdict": "refuse",
          "because": "a reason a reviewer can weigh"
        }
        """;

    private const string Playbook = """
        { "format": { "version": 0, "requires": [], "uses": [] } }
        """;

    [Fact]
    public void Read_ACase_CarriesItsFixtureAndTheDocumentItIsAbout()
    {
        using var corpus = Holding("unknown-requires");

        var aCase = new ReadableCorpus(corpus.Folder).Read("unknown-requires");

        Assert.Equal("unknown-requires", aCase.Name);
        Assert.Equal(Verdict.Refuse, aCase.Fixture.Verdict);
        Assert.Equal(Playbook, aCase.Playbook);
    }

    [Fact]
    public void Read_ACaseWhoseFixtureIsMalformed_SaysWhichCase()
    {
        using var corpus = new TemporaryCorpus()
            .With("broken", ("fixture.json", "{ not a fixture"), ("playbook.json", Playbook));

        var error = Assert.Throws<InvalidFixtureException>(() => new ReadableCorpus(corpus.Folder).Read("broken"));

        Assert.Contains("broken", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ACaseWhosePlaybookIsMissing_SaysWhichCaseAndWhichFile()
    {
        using var corpus = new TemporaryCorpus().With("no-playbook", ("fixture.json", Fixture));

        var error = Assert.Throws<InvalidFixtureException>(() => new ReadableCorpus(corpus.Folder).Read("no-playbook"));

        Assert.Contains("no-playbook", error.Message, StringComparison.Ordinal);
        Assert.Contains("playbook.json", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_AFixtureNamingAnotherPlaybook_FollowsTheNameRatherThanAssumingOne()
    {
        // The document is named by the fixture rather than fixed by convention, so a case is free
        // to point at one under any name -- and must be told when that name leads nowhere.
        using var corpus = new TemporaryCorpus().With(
            "renamed",
            ("fixture.json", Fixture.Replace("playbook.json", "elsewhere.json", StringComparison.Ordinal)),
            ("playbook.json", Playbook));

        var error = Assert.Throws<InvalidFixtureException>(() => new ReadableCorpus(corpus.Folder).Read("renamed"));

        Assert.Contains("elsewhere.json", error.Message, StringComparison.Ordinal);
    }



    private static TemporaryCorpus Holding(string caseName) =>
        new TemporaryCorpus().With(caseName, ("fixture.json", Fixture), ("playbook.json", Playbook));
}
