using CsCheck;
using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Emission;

/// <summary>
/// The playbook round-trip: what <c>PlaybookWriter</c> writes, <c>PlaybookReader</c> reads back
/// unchanged, for every script the compiler accepts.
/// </summary>
/// <remarks>
/// Equality is taken over the serialized JSON rather than over <see cref="PlaybookDocument"/>
/// itself: the document holds its nodes and speakers in <c>ImmutableArray</c>, which compares by
/// reference, so two structurally identical documents are never equal.
/// </remarks>
public sealed class PlaybookRoundTripTests
{
    // Modest so the property runs in the ordinary suite.
    private const int Samples = 200;

    private const string ScriptName = "generated.dialogue.md";

    /// <summary>
    /// Writing a playbook, reading it back, and writing it again yields the same document.
    /// </summary>
    [Fact]
    public void WritingAPlaybookAndReadingItBackPreservesIt() =>
        ForEveryCompiledScript(
            (compilation, source) =>
            {
                var written = Playbooks.Serialize(Write(compilation));
                var reread = Playbooks.Serialize(PlaybookReader.Default.Read(written));

                Assert.True(
                    written == reread,
                    $"A playbook changed when it was read back and written again.{Environment.NewLine}"
                        + $"Script:{Environment.NewLine}{source}{Environment.NewLine}"
                        + $"Written:{Environment.NewLine}{written}{Environment.NewLine}"
                        + $"Re-written:{Environment.NewLine}{reread}");
            });

    /// <summary>
    /// A playbook the writer produced is one the reader accepts.
    /// </summary>
    /// <remarks>
    /// The reader throws <see cref="InvalidPlaybookException"/> on a document it judges malformed;
    /// this reports that as a rejection rather than as a crash inside the round-trip property.
    /// </remarks>
    [Fact]
    public void EveryPlaybookTheWriterProducesIsOneTheReaderAccepts() =>
        ForEveryCompiledScript(
            (compilation, source) =>
            {
                var written = Playbooks.Serialize(Write(compilation));

                var rejection = Record.Exception(() => PlaybookReader.Default.Read(written));

                Assert.True(
                    rejection is null,
                    $"The reader rejected a playbook the writer produced: {rejection?.Message}"
                        + $"{Environment.NewLine}Script:{Environment.NewLine}{source}"
                        + $"{Environment.NewLine}Playbook:{Environment.NewLine}{written}");
            });

    private static PlaybookDocument Write(CompilationSuccess compilation) =>
        PlaybookWriterFactory.CreateDefault().Write(compilation, ScriptName);

    // Only a script the compiler accepts has a playbook; a rejected one is skipped.
    private static void ForEveryCompiledScript(Action<CompilationSuccess, string> invariantHolds) =>
        ScriptGen.Script()
            .Sample(
                source =>
                {
                    if (ScriptCompilerFactory.CreateDefault().Compile(source) is CompilationSuccess success)
                    {
                        invariantHolds(success, source);
                    }
                },
                iter: Samples);
}
