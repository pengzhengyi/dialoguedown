using DialogueDown.Conformance;
using DialogueDown.Conformance.Authoring;
using DialogueDown.Playbook;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Conformance;

/// <summary>
/// The script under a refused case's <c>broken:</c> block is the accepted document the case was
/// broken from: it compiles, the reader accepts it, and it is not the case's own playbook.
/// </summary>
/// <remarks>
/// This is the readable half's counterpart to <see cref="PlayableCaseTests"/>. Where a playable
/// case's playbook must equal its source compile, a readable refusal's must differ from it: the
/// source is that compile, and the case is the one deliberate edit. Compiling the source proves the
/// case is an otherwise-sound document, and the difference proves the edit was actually made.
/// </remarks>
public sealed class RefusedSourceTests
{
    private const string SourceFile = "source.dialogue.md";
    private const string PlaybookFile = "playbook.json";

    public static TheoryData<ReadableCase> Refusals() =>
        [.. Corpora.Readable.Cases().Where(aCase => aCase.WillRefuse)];

    [Theory]
    [MemberData(nameof(Refusals))]
    public void ARefusalsSource_CompilesToAValidPlaybookOtherThanItsOwn(ReadableCase aCase)
    {
        var source = Corpora.ReadableFolder.Read(aCase.Name, SourceFile);
        var committed = Corpora.ReadableFolder.Read(aCase.Name, PlaybookFile);

        var compiled = Playbooks.Serialize(Playbooks.Of(BrokenBlock.Parse(source).Script, SourceFile));

        // The source is a document the reader takes ...
        PlaybookReader.Default.Read(compiled);

        // ... and the case is broken, not a second copy of that document.
        Assert.NotEqual(committed, compiled);
    }
}
