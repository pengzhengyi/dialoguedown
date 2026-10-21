using DialogueDown.Conformance;
using DialogueDown.Conformance.Authoring;
using DialogueDown.Playbook;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Conformance;

/// <summary>
/// A playable case's playbook must be exactly what its source compiles to, unless the case holds a
/// playbook no compiler can emit.
/// </summary>
/// <remarks>
/// A committed playbook that no longer matches its source asserts an outdated format. Only playable
/// cases are checked this way: a readable case's playbook is deliberately that compile with one
/// field broken.
/// <para>
/// Some runtime behavior can only be reached by a playbook the compiler rejects the script for,
/// such as one key a random choice needs as a truth and as a number. Such a case is hand-edited:
/// its source opens with a <c>broken:</c> block naming the edit, as a readable refusal's does, and
/// its playbook is the compile of the script below with that one edit made.
/// </para>
/// </remarks>
public sealed class PlayableCaseTests
{
    private const string SourceFile = "source.dialogue.md";
    private const string PlaybookFile = "playbook.json";

    public static TheoryData<string> CompiledCases() =>
        [.. Corpora.PlayableFolder.Cases().Where(caseName => !IsHandEdited(caseName))];

    public static TheoryData<string> HandEditedCases() => [.. Corpora.PlayableFolder.Cases().Where(IsHandEdited)];

    [Fact]
    public void ThePlayableHalf_HasCases()
    {
        // Without this, emptying the half would leave the theory below passing on nothing.
        Assert.NotEmpty(CompiledCases());
    }

    [Theory]
    [MemberData(nameof(CompiledCases))]
    public void ACase_ShipsThePlaybookItsSourceCompilesTo(string caseName)
    {
        var source = Corpora.PlayableFolder.Read(caseName, SourceFile);

        var recompiled = Playbooks.Serialize(Playbooks.Of(source, SourceFile));

        Assert.Equal(Corpora.PlayableFolder.Read(caseName, PlaybookFile), recompiled);
    }

    [Theory]
    [MemberData(nameof(HandEditedCases))]
    public void AHandEditedCasesSource_CompilesToAValidPlaybookOtherThanItsOwn(string caseName)
    {
        var source = Corpora.PlayableFolder.Read(caseName, SourceFile);
        var committed = PlaybookReader.Default.Read(Corpora.PlayableFolder.Read(caseName, PlaybookFile));

        // The script below the block is a document the reader takes ...
        var compiled = PlaybookReader.Default.Read(
            Playbooks.Serialize(Playbooks.Of(BrokenBlock.Parse(source).Script, SourceFile)));

        // ... and the case is the edit a compiler would not make, not a second copy of that document.
        // Comparing documents rather than text keeps a reformatted copy from passing as an edit.
        Assert.NotEqual(committed, compiled);
    }

    private static bool IsHandEdited(string caseName) =>
        Corpora.PlayableFolder.Read(caseName, SourceFile).StartsWith(BrokenBlock.Marker, StringComparison.Ordinal);
}
