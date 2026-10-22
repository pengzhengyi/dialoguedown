using static DialogueDown.Conformance.Tests.Support.FixtureJsonFactory;
using static DialogueDown.Conformance.Tests.Support.InvalidFixtureAssert;
using static DialogueDown.Conformance.Tests.Support.SessionEntryAssert;

namespace DialogueDown.Conformance;

public sealed class PlayableCorpusTests
{
    [Fact]
    public void Read_ACase_CarriesItsFixtureAndTheDocumentItIsAbout()
    {
        using var corpus = Holding("a-case");

        var aCase = new PlayableCorpus(corpus.Folder).Read("a-case");

        Assert.Equal("a-case", aCase.Name);
        AssertSends(Assert.Single(aCase.Fixture.Session), "\"next\"");
        Assert.Equal(APlaybook, aCase.Playbook);
    }

    [Fact]
    public void Read_ACaseWhoseFixtureIsMalformed_SaysWhichCase()
    {
        using var corpus = new TemporaryCorpus()
            .With("broken", ("fixture.json", "{ not a fixture"), ("playbook.json", APlaybook));

        var error = AssertInvalid(() => new PlayableCorpus(corpus.Folder).Read("broken"));

        // What follows the case is the JSON reader's own account of the problem.
        Assert.StartsWith(
            "The case 'broken' has a bad fixture.json: This is not a playable fixture: ",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ACaseWhosePlaybookIsMissing_SaysWhichCaseAndWhichFile()
    {
        using var corpus = new TemporaryCorpus()
            .With("no-playbook", ("fixture.json", APlayableFixture().ToJsonString()));

        var error = AssertInvalid(() => new PlayableCorpus(corpus.Folder).Read("no-playbook"));

        Assert.Equal("The case 'no-playbook' has no playbook.json.", error.Message);
    }

    [Fact]
    public void Read_AFixtureNamingAnotherPlaybook_FollowsTheNameRatherThanAssumingOne()
    {
        using var corpus = new TemporaryCorpus().With(
            "renamed",
            ("fixture.json", APlayableFixture().WithField("playbook", "elsewhere.json")),
            ("playbook.json", APlaybook));

        var error = AssertInvalid(() => new PlayableCorpus(corpus.Folder).Read("renamed"));

        Assert.Equal("The case 'renamed' has no elsewhere.json.", error.Message);
    }

    [Fact]
    public void Cases_ReadEveryCaseName()
    {
        using var corpus = Holding("a-case");
        var reader = new PlayableCorpus(corpus.Folder);

        Assert.Equal(["a-case"], reader.CaseNames());
        Assert.Equal("a-case", Assert.Single(reader.Cases()).Name);
    }

    private static TemporaryCorpus Holding(string caseName) =>
        new TemporaryCorpus().With(
            caseName, ("fixture.json", APlayableFixture().ToJsonString()), ("playbook.json", APlaybook));
}
