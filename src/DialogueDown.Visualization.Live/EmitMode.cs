using DialogueDown.Configuration;
using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// The <c>visualize &lt;file&gt; --emit &lt;format&gt;</c> path: compile the document
/// and write every stage's graph as DOT text, to a file or standard output.
/// A non-interactive emit for embedding a graph elsewhere — no server, no browser.
/// </summary>
internal static class EmitMode
{
    /// <summary>
    /// Renders <paramref name="file"/> as <paramref name="format"/> text, compiled with the
    /// project's <paramref name="options"/>. Writes to <paramref name="output"/> when given,
    /// otherwise to <paramref name="writer"/> (standard output). A bad document writes the
    /// problem to <paramref name="error"/> and returns 1, before any text is emitted. Returns a
    /// process exit code.
    /// </summary>
    public static int Run(
        string file,
        EmitFormat format,
        string? output,
        CompilerOptions options,
        TextWriter writer,
        TextWriter error)
    {
        var problem = DocumentValidation.Validate(file);
        if (problem is not null)
        {
            error.WriteLine(problem);
            return 1;
        }

        var text = new CompilationVisualizer(options).RenderText(File.ReadAllText(file), format);
        if (output is null)
        {
            writer.Write(text);
        }
        else
        {
            File.WriteAllText(output, text);
        }

        return 0;
    }
}
