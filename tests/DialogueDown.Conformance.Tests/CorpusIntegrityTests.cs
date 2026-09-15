namespace DialogueDown.Conformance;

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
    private const string SourceFile = "source.dialogue.md";
    private const string BrokenMarker = "<!-- broken:";

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

    public static TheoryData<ReadableCase> Refusals() =>
        [.. Corpora.Readable.Cases().Where(aCase => aCase.WillRefuse)];

    public static TheoryData<ReadableCase> Acceptances() =>
        [.. Corpora.Readable.Cases().Where(aCase => aCase.WillAccept)];

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

    [Theory]
    [MemberData(nameof(Refusals))]
    public void ARefusalsSource_OpensWithAWellFormedBrokenBlock(ReadableCase aCase)
    {
        var source = Corpora.ReadableFolder.Read(aCase.Name, SourceFile);

        Assert.StartsWith(BrokenMarker, source, StringComparison.Ordinal);

        var close = source.IndexOf("-->", StringComparison.Ordinal);
        Assert.True(close > BrokenMarker.Length, $"The case '{aCase}' has no closed `broken:` block.");

        var note = source[BrokenMarker.Length..source.IndexOf('\n')].Trim();
        Assert.NotEmpty(note);

        // The block must hold no second marker, or the parser would close it early and leak the
        // rest into the script. A blank line separates the note from the evidence.
        var body = source[BrokenMarker.Length..close];
        Assert.DoesNotContain("--", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<!--", body, StringComparison.Ordinal);
        Assert.Contains("\n\n", body, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Acceptances))]
    public void AnAcceptancesSource_CarriesNoBrokenBlock(ReadableCase aCase)
    {
        var source = Corpora.ReadableFolder.Read(aCase.Name, SourceFile);

        Assert.DoesNotContain(BrokenMarker, source, StringComparison.Ordinal);
    }
}
