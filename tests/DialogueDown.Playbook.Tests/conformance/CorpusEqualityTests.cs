using System.Text.Json;
using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// Reads every playbook the conformance corpus offers twice, and asserts the two are one value.
/// </summary>
/// <remarks>
/// The corpus is the project's normative set of real documents, so this keeps value equality honest
/// as those documents grow. It complements the constructed comprehensive document in
/// <see cref="EqualityTests"/>, which pins that every construct kind is compared by value today;
/// the two cover different halves of the same contract.
/// </remarks>
public sealed class CorpusEqualityTests
{
    public static TheoryData<ReadableCase> ReadableAccepted() =>
        [.. Corpora.Readable.Cases().Where(aCase => aCase.WillAccept)];

    public static TheoryData<string> Playable() =>
        [.. Corpora.PlayableFolder.Cases()];

    [Theory]
    [MemberData(nameof(ReadableAccepted))]
    public void AReadableCaseTheCorpusAccepts_ReadTwice_IsOneValue(ReadableCase aCase) =>
        PlaybookJsonAssert.AssertReadsTwiceAsOneValue<PlaybookDocument>(aCase.Playbook);

    [Theory]
    [MemberData(nameof(Playable))]
    public void APlayableCase_ReadTwice_IsOneValue(string caseName) =>
        PlaybookJsonAssert.AssertReadsTwiceAsOneValue<PlaybookDocument>(ReadPlaybook(caseName));

    [Fact]
    public void TheCorpus_OffersPlaybooksToCompare()
    {
        // Without this, a corpus that went empty would turn every theory into a silent pass.
        Assert.NotEmpty(ReadableAccepted());
        Assert.NotEmpty(Playable());
    }

    private static string ReadPlaybook(string caseName)
    {
        var fixture = JsonDocument.Parse(Corpora.PlayableFolder.Read(caseName, "fixture.json"));
        var playbook = fixture.RootElement.GetProperty("playbook").GetString()!;

        return Corpora.PlayableFolder.Read(caseName, playbook);
    }
}
