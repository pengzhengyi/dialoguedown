using static DialogueDown.Conformance.Tests.Support.FixtureJsonFactory;
using static DialogueDown.Conformance.Tests.Support.InvalidFixtureAssert;

namespace DialogueDown.Conformance;

public sealed class ReadableCorpusTests
{
    [Fact]
    public void Read_ACase_CarriesItsFixtureAndTheDocumentItIsAbout()
    {
        using var corpus = Holding("unknown-requires");

        var aCase = new ReadableCorpus(corpus.Folder).Read("unknown-requires");

        Assert.Equal("unknown-requires", aCase.Name);
        Assert.Equal(Verdict.Refuse, aCase.Fixture.Verdict);
        Assert.Equal(APlaybook, aCase.Playbook);
    }

    [Fact]
    public void Read_ACaseWhoseFixtureIsMalformed_SaysWhichCase()
    {
        using var corpus = new TemporaryCorpus()
            .With("broken", ("fixture.json", "{ not a fixture"), ("playbook.json", APlaybook));

        AssertInvalid(() => new ReadableCorpus(corpus.Folder).Read("broken"), "broken");
    }

    [Fact]
    public void Read_ACaseWhosePlaybookIsMissing_SaysWhichCaseAndWhichFile()
    {
        using var corpus = new TemporaryCorpus()
            .With("no-playbook", ("fixture.json", AReadableFixture().ToJsonString()));

        AssertInvalid(() => new ReadableCorpus(corpus.Folder).Read("no-playbook"), "no-playbook", "playbook.json");
    }

    [Fact]
    public void Read_AFixtureNamingAnotherPlaybook_FollowsTheNameRatherThanAssumingOne()
    {
        // The fixture names the document, so a case may use any file name, and a name with no
        // file behind it is reported.
        using var corpus = new TemporaryCorpus().With(
            "renamed",
            ("fixture.json", AReadableFixture().WithField("playbook", "elsewhere.json")),
            ("playbook.json", APlaybook));

        AssertInvalid(() => new ReadableCorpus(corpus.Folder).Read("renamed"), "elsewhere.json");
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
        new TemporaryCorpus().With(
            caseName, ("fixture.json", AReadableFixture().ToJsonString()), ("playbook.json", APlaybook));
}
