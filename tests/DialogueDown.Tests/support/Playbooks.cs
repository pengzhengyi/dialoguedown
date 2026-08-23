using System.Text.Json;
using DialogueDown.Emission;
using DialogueDown.Playbook;

namespace DialogueDown.Tests.Support;

/// <summary>Playbooks built the way the CLI builds them, for tests about what comes out.</summary>
internal static class Playbooks
{
    /// <summary>The playbook a script compiles to.</summary>
    /// <param name="source">The script to compile.</param>
    /// <param name="script">The name the playbook should report.</param>
    /// <returns>The playbook, as a successful compile produces it.</returns>
    public static PlaybookDocument Of(string source, string script) =>
        PlaybookWriterFactory.CreateDefault().Write(Pipeline.Compiled(source), script);

    /// <summary>A playbook as it lands in a file.</summary>
    /// <param name="playbook">The playbook to write out.</param>
    /// <returns>Its JSON, byte for byte as the CLI would write it.</returns>
    public static string Serialize(PlaybookDocument playbook) =>
        JsonSerializer.Serialize(playbook, PlaybookJson.Options);
}
