using DialogueDown.Conformance;
using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// Reads every playbook the conformance corpus offers twice, and asserts the two are one value.
/// </summary>
/// <remarks>
/// The corpus holds the project's real documents, so this check grows as they do.
/// </remarks>
public sealed class CorpusEqualityTests
{
    public static TheoryData<ReadableCase> ReadableAccepted() =>
        [.. Corpora.Readable.Cases().Where(aCase => aCase.WillAccept)];

    public static TheoryData<PlayableCase> Playable() =>
        [.. Corpora.Playable.Cases()];

    [Theory]
    [MemberData(nameof(ReadableAccepted))]
    public void AReadableCaseTheCorpusAccepts_ReadTwice_IsOneValue(ReadableCase aCase) =>
        PlaybookJsonAssert.AssertReadsTwiceAsOneValue<PlaybookDocument>(aCase.Playbook);

    [Theory]
    [MemberData(nameof(Playable))]
    public void APlayableCase_ReadTwice_IsOneValue(PlayableCase aCase) =>
        PlaybookJsonAssert.AssertReadsTwiceAsOneValue<PlaybookDocument>(aCase.Playbook);

    [Fact]
    public void TheCorpus_OffersPlaybooksToCompare()
    {
        // Without this, a corpus that went empty would turn every theory into a silent pass.
        Assert.NotEmpty(ReadableAccepted());
        Assert.NotEmpty(Playable());
    }
}
