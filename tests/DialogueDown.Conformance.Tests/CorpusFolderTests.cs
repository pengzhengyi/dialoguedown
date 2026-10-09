using static DialogueDown.Conformance.Tests.Support.InvalidFixtureAssert;

namespace DialogueDown.Conformance;

public sealed class CorpusFolderTests
{
    private const string Anything = "whatever a case happens to hold";

    [Fact]
    public void Cases_ACorpus_NamesEveryCaseInAStableOrder()
    {
        using var corpus = new TemporaryCorpus()
            .With("beta", ("playbook.json", Anything))
            .With("alpha", ("playbook.json", Anything));

        Assert.Equal(["alpha", "beta"], corpus.Folder.Cases());
    }

    [Fact]
    public void Read_AFile_ComesBackExactlyAsWritten()
    {
        using var corpus = new TemporaryCorpus().With("a-case", ("playbook.json", Anything));

        Assert.Equal(Anything, corpus.Folder.Read("a-case", "playbook.json"));
    }

    [Fact]
    public void Read_ACaseThatIsNotThere_SaysWhichCase()
    {
        using var corpus = new TemporaryCorpus().With("here", ("playbook.json", Anything));

        AssertInvalid(() => corpus.Folder.Read("elsewhere", "playbook.json"), "elsewhere");
    }

    [Fact]
    public void Read_AFileThatIsNotThere_SaysWhichCaseAndWhichFile()
    {
        using var corpus = new TemporaryCorpus().With("a-case", ("playbook.json", Anything));

        AssertInvalid(() => corpus.Folder.Read("a-case", "fixture.json"), "a-case", "fixture.json");
    }

    [Fact]
    public void Has_AFileTheCaseShips_IsTrue()
    {
        using var corpus = new TemporaryCorpus().With("a-case", ("source.dialogue.md", Anything));

        Assert.True(corpus.Folder.Has("a-case", "source.dialogue.md"));
    }

    [Fact]
    public void Has_AFileTheCaseDoesNotShip_IsFalse()
    {
        using var corpus = new TemporaryCorpus().With("a-case", ("playbook.json", Anything));

        Assert.False(corpus.Folder.Has("a-case", "source.dialogue.md"));
    }
}
