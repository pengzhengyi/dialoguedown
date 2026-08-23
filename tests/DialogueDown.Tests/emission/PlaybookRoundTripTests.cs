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
/// A playbook is a persisted artifact — the one way out of the compiler — so a writer's work
/// survives only if what was written reads back as what it was. The goldens pin what four shipped
/// examples produce; this quantifies over scripts nobody wrote, which is where a field the reader
/// silently drops would hide.
/// <para>
/// Equality is taken over the serialized JSON rather than over <see cref="PlaybookDocument"/>
/// itself. The document holds its nodes and speakers in <c>ImmutableArray</c>, whose record
/// equality compares the underlying array by reference, so two structurally identical documents
/// are never equal. Re-serializing renders both through the same writer and compares what a file
/// would actually hold.
/// </para>
/// </remarks>
public sealed class PlaybookRoundTripTests
{
    // Modest on purpose: this runs in the ordinary suite, and a property that makes the suite slow
    // stops being run at all.
    private const int Samples = 200;

    private const string ScriptName = "generated.dialogue.md";

    /// <summary>
    /// Writing a playbook, reading it back, and writing it again yields the same document.
    /// </summary>
    /// <remarks>
    /// A failure names one of two defects, and the diff says which: the reader dropped or
    /// misread something the writer emitted, or the writer emitted something the reader cannot
    /// express. Either way a runtime loading that file plays something the compiler did not mean.
    /// </remarks>
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
    /// The reader validates what it loads and throws <see cref="InvalidPlaybookException"/> on a
    /// document it judges malformed. Nothing the writer produces should ever be judged that way —
    /// if it is, the two sides disagree about the format they share, and the round-trip property
    /// above would report it as an unrelated crash rather than as the disagreement it is.
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

    // Only a script the compiler accepts has a playbook. One it rejects never reaches the writer,
    // and so says nothing either way about an invariant quantified over playbooks.
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
