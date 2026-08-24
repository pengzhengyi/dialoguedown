namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// The corpus this build ships, resolved once from where the build put it.
/// </summary>
/// <remarks>
/// The one place that knows the corpus's layout on disk. Keeping it here leaves
/// <see cref="CorpusFolder"/> a plain folder of cases and <see cref="ReadableCorpus"/> a reader of
/// them, neither carrying a path that only this repository's build could satisfy.
/// <para>
/// The folders are declared before what reads them, because a static property initializer that
/// reached forward to a later one would quietly see null.
/// </para>
/// </remarks>
internal static class Corpora
{
    /// <summary>Gets the readable half: can a reader load this document at all.</summary>
    public static CorpusFolder ReadableFolder { get; } = Half("readable");

    /// <summary>Gets the playable half: does a runner hold the same conversation.</summary>
    public static CorpusFolder PlayableFolder { get; } = Half("playable");

    /// <summary>Gets the readable half, read as cases rather than as files.</summary>
    public static ReadableCorpus Readable { get; } = new(ReadableFolder);

    /// <summary>Both halves, for the checks that hold across the whole corpus.</summary>
    /// <returns>Each half, as a folder of cases.</returns>
    public static IEnumerable<CorpusFolder> Halves() => [ReadableFolder, PlayableFolder];

    private static CorpusFolder Half(string half) =>
        new(Path.Combine(AppContext.BaseDirectory, "conformance", half));
}
