namespace DialogueDown.Conformance;

/// <summary>
/// The corpus this build ships, resolved once from where the build put it.
/// </summary>
/// <remarks>
/// The one place that knows where the build puts the corpus on disk.
/// <para>
/// The folders are declared before the corpora that read them, because a static property
/// initializer that reads a later one sees null.
/// </para>
/// </remarks>
public static class Corpora
{
    /// <summary>Gets the readable half: can a reader load this document at all.</summary>
    public static CorpusFolder ReadableFolder { get; } = Half("readable");

    /// <summary>Gets the playable half: does a runner hold the same conversation.</summary>
    public static CorpusFolder PlayableFolder { get; } = Half("playable");

    /// <summary>Gets the readable half, read as cases rather than as files.</summary>
    public static ReadableCorpus Readable { get; } = new(ReadableFolder);

    /// <summary>Gets the playable half, read as cases rather than as files.</summary>
    public static PlayableCorpus Playable { get; } = new(PlayableFolder);

    /// <summary>Both halves, for the checks that hold across the whole corpus.</summary>
    /// <returns>Each half, as a folder of cases.</returns>
    public static IEnumerable<CorpusFolder> Halves() => [ReadableFolder, PlayableFolder];

    private static CorpusFolder Half(string half) =>
        new(Path.Combine(AppContext.BaseDirectory, "conformance", half));
}
