namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// What must hold of every case in the corpus, in either half.
/// </summary>
/// <remarks>
/// The harness reads only a fixture and the document it names, so nothing else would notice a case
/// that quietly lost a file -- least of all in the playable half, which has no runner to run it
/// until C2 arrives.
/// </remarks>
public sealed class CorpusIntegrityTests
{
    private static readonly string[] _everyCaseShips =
        ["fixture.json", "playbook.json", "source.dialogue.md"];

    public static TheoryData<string, string> EveryCase()
    {
        var cases = new TheoryData<string, string>();

        foreach (var half in Corpora.Halves())
        {
            foreach (var caseName in half.Cases())
            {
                cases.Add(half.Name, caseName);
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(EveryCase))]
    public void ACase_ShipsAFixtureAPlaybookAndTheSourceItCameFrom(string half, string caseName)
    {
        var folder = Corpora.Halves().Single(candidate => candidate.Name == half);

        foreach (var file in _everyCaseShips)
        {
            Assert.True(folder.Has(caseName, file), $"The case '{half}/{caseName}' ships no {file}.");
        }
    }

    [Fact]
    public void EveryHalf_HasCases()
    {
        // Without this, emptying a half would leave its share of the theory above passing on
        // nothing at all.
        Assert.All(Corpora.Halves(), half => Assert.NotEmpty(half.Cases()));
    }
}
