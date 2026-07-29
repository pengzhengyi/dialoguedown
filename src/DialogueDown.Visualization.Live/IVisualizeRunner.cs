using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// Drives the non-interactive visualization outputs for the <c>dialoguedown visualize</c>
/// command: a one-shot static HTML export, or a text emit of each stage's graph. The served
/// View/Edit shell is driven separately through <see cref="ILauncherRunner"/>. Injected so the
/// command is testable with a substitute.
/// </summary>
public interface IVisualizeRunner
{
    /// <summary>
    /// Renders <paramref name="file"/> to a self-contained report and opens it (unless
    /// <paramref name="noOpen"/>), or writes it to <paramref name="output"/>, showing the
    /// applied <paramref name="configuration"/> in the Config tab. Returns a process exit code.
    /// </summary>
    int RunStatic(string file, string? output, bool noOpen, AppliedConfiguration configuration);

    /// <summary>
    /// Renders every stage of <paramref name="file"/> as text in the given
    /// <paramref name="format"/> (Mermaid or DOT), using the project's
    /// <paramref name="options"/>, and writes it to <paramref name="output"/>, or to
    /// standard output when null. A non-interactive emit — no server, no browser.
    /// Returns a process exit code.
    /// </summary>
    int RunEmit(string file, EmitFormat format, string? output, CompilerOptions options);
}
