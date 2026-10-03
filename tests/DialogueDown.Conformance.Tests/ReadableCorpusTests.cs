
namespace DialogueDown.Conformance;

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
        // The fixture names the document, so a case may use any file name, and a name with no
        // file behind it is reported.
        using var corpus = new TemporaryCorpus().With(
            "renamed",
            ("fixture.json", Fixture.Replace("playbook.json", "elsewhere.json", StringComparison.Ordinal)),
            ("playbook.json", Playbook));

        var error = Assert.Throws<InvalidFixtureException>(() => new ReadableCorpus(corpus.Folder).Read("renamed"));

        Assert.Contains("elsewhere.json", error.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Cases_ReadEveryCaseName()
    {
        using var corpus = Holding("a-case");
        var reader = new ReadableCorpus(corpus.Folder);

        Assert.Equal(["a-case"], reader.CaseNames());
        Assert.Equal("a-case", Assert.Single(reader.Cases()).Name);
    }

    private static TemporaryCorpus Holding(string caseName) =>
        new TemporaryCorpus().With(caseName, ("fixture.json", Fixture), ("playbook.json", Playbook));
}
