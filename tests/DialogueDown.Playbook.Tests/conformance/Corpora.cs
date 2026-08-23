namespace DialogueDown.Playbook.Tests.Conformance;

/// <summary>
/// The corpora this build ships, resolved once from where the build put them.
/// </summary>
/// <remarks>
/// The one place that knows the corpus's layout on disk. Keeping it here leaves
/// <see cref="CorpusFolder"/> a plain folder of cases and <see cref="ReadableCorpus"/> a reader of
/// them, neither carrying a path that only this repository's build could satisfy.
/// </remarks>
internal static class Corpora
{
    /// <summary>Gets the readable half: can a reader load this document at all.</summary>
    public static ReadableCorpus Readable { get; } = new(Half("readable"));

    private static CorpusFolder Half(string half) =>
        new(Path.Combine(AppContext.BaseDirectory, "conformance", half));
}
