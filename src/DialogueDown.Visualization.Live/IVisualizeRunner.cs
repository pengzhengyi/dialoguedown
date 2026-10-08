using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;

using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// Drives the CLI's non-interactive visualization outputs: the static HTML export of
/// <c>ddown visualize -o</c>, and the stage-graph text of <c>ddown compile --emit</c>. The served
/// View/Edit shell is driven separately through <see cref="IServedShellRunner"/>. Injected so the
/// commands are testable with a substitute.
/// </summary>
public interface IVisualizeRunner
{
    /// <summary>
    /// Renders <paramref name="file"/> to a self-contained report, written to
    /// <paramref name="output"/> or else a temporary file, and opens it unless
    /// <paramref name="noOpen"/>; the report's Config tab shows the applied
    /// <paramref name="configuration"/>. Returns a process exit code.
    /// </summary>
    int RunStatic(string file, string? output, bool noOpen, AppliedConfiguration configuration);

    /// <summary>
    /// Renders every stage of <paramref name="file"/> as text in the given
    /// <paramref name="format"/>, using the project's <paramref name="options"/>, and writes it
    /// to <paramref name="output"/>, or to standard output when null. A non-interactive emit — no
    /// server, no browser. Returns a process exit code.
    /// </summary>
    int RunEmit(string file, EmitFormat format, string? output, CompilerOptions options);
}
