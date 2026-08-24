namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// Runs the shipped corpus against this build's reader. Every other runtime is expected to reach
/// the same verdicts from the same files, which is the whole point of keeping them as data.
/// </summary>
public sealed class ReadableConformanceTests
{
    private static ReadableCorpus Corpus => Corpora.Readable;

    public static TheoryData<string> Accepted() => Cases(Verdict.Accept);

    public static TheoryData<string> Refused() => Cases(Verdict.Refuse);

    [Theory]
    [MemberData(nameof(Accepted))]
    public void ACaseTheCorpusAccepts_IsRead(string caseName)
    {
        var aCase = Corpus.Read(caseName);

        var playbook = PlaybookReader.Default.Read(aCase.Playbook);

        Assert.NotEmpty(playbook.Nodes);
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public void ACaseTheCorpusRefuses_IsNotRead(string caseName)
    {
        var aCase = Corpus.Read(caseName);

        Assert.Throws<InvalidPlaybookException>(() => PlaybookReader.Default.Read(aCase.Playbook));
    }

    [Fact]
    public void TheCorpus_CoversBothVerdicts()
    {
        // Without this, emptying the corpus would turn every theory above into a silent pass --
        // a suite that reports green because it asked nothing.
        Assert.NotEmpty(Accepted());
        Assert.NotEmpty(Refused());
    }


    private static TheoryData<string> Cases(Verdict verdict)
    {
        var cases = new TheoryData<string>();

        foreach (var caseName in Corpus.Cases())
        {
            if (Corpus.Read(caseName).Fixture.Verdict == verdict)
            {
                cases.Add(caseName);
            }
        }

        return cases;
    }
}
