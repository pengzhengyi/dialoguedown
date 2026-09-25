using Generator.Equals;

namespace DialogueDown.Diagnostics;

/// <summary>
/// A suggested repair for a diagnostic: a writer-facing <see cref="Title"/> and the
/// <see cref="Edits"/> that apply it. The producer attaches it where the diagnostic is made, so
/// every consumer either forwards it or ignores it and no one re-derives the repair.
/// </summary>
[Equatable]
internal sealed partial record DiagnosticFix
{
    public DiagnosticFix(string title, IReadOnlyList<DiagnosticEdit> edits)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentNullException.ThrowIfNull(edits);
        if (edits.Count == 0)
        {
            throw new ArgumentException("A fix must carry at least one edit.", nameof(edits));
        }

        Title = title;
        Edits = edits;
    }

    /// <summary>The action's label, written for the writer.</summary>
    public string Title { get; }

    /// <summary>The edits that apply the repair, in application order.</summary>
    [OrderedEquality]
    public IReadOnlyList<DiagnosticEdit> Edits { get; }
}
