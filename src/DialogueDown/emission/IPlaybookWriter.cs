using DialogueDown.Compilation;
using DialogueDown.Playbook;

namespace DialogueDown.Emission;

/// <summary>
/// Lowers a compiled script into a playbook — the portable form a runtime loads.
/// </summary>
/// <remarks>
/// The compiler's graph is internal, and a playbook is the only form a runtime receives, so a
/// construct a playbook cannot express is one no runtime can play.
/// </remarks>
public interface IPlaybookWriter
{
    /// <summary>
    /// Writes a compiled script as a playbook.
    /// </summary>
    /// <param name="compilation">The compile to write. Only a successful compile has a graph.</param>
    /// <param name="script">
    /// The name of the script this was compiled from, such as <c>intro.dialogue.md</c>, as a
    /// runtime should report it. A compile does not know where its source came from, so the
    /// caller supplies it.
    /// </param>
    /// <returns>The playbook, ready to serialize.</returns>
    PlaybookDocument Write(CompilationSuccess compilation, string script);
}
