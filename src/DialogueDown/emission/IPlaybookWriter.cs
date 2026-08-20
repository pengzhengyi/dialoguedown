using DialogueDown.Compilation;
using DialogueDown.Playbook;

namespace DialogueDown.Emission;

/// <summary>
/// Lowers a compiled script into a playbook — the portable form a runtime loads.
/// </summary>
/// <remarks>
/// The compiler ends at a graph it keeps to itself. This is the one way out of the process, so
/// whatever a playbook cannot express is a construct no runtime can play.
/// </remarks>
public interface IPlaybookWriter
{
    /// <summary>
    /// Writes a compiled script as a playbook.
    /// </summary>
    /// <param name="compilation">The compile to write. Only a successful compile has a graph.</param>
    /// <param name="script">
    /// The script this was compiled from, as a runtime should report it. A compile does not know
    /// where its source came from, so whoever opened it says.
    /// </param>
    /// <returns>The playbook, ready to serialize.</returns>
    PlaybookDocument Write(CompilationSuccess compilation, string script);
}
