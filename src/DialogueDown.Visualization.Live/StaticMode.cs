using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Live.Launching;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// The <c>visualize &lt;file&gt;</c> path: compile the document and write a
/// self-contained report, then open it in the browser unless suppressed. This is
/// the offline artifact — no server.
/// </summary>
internal static class StaticMode
{
    /// <summary>
    /// Renders <paramref name="file"/> to a self-contained report, showing the applied
    /// <paramref name="configuration"/> in the Config tab. Writes to <paramref name="output"/>
    /// when given, otherwise a temp file; opens it via <paramref name="browser"/> unless
    /// <paramref name="noOpen"/> is set. Returns a process exit code (0 on success, 1 on a bad
    /// document).
    /// </summary>
    public static int Run(
        string file,
        string? output,
        bool noOpen,
        AppliedConfiguration configuration,
        IBrowserLauncher browser,
        TextWriter error)
    {
        var problem = DocumentValidation.Validate(file);
        if (problem is not null)
        {
            error.WriteLine(problem);
            return 1;
        }

        var html = new CompilationVisualizer(configuration).RenderHtmlReport(File.ReadAllText(file), file);
        var target = output ?? Path.Combine(Path.GetTempPath(), $"dd-report-{Guid.NewGuid():N}.html");
        File.WriteAllText(target, html);

        if (!noOpen)
        {
            browser.Open(target);
        }

        return 0;
    }
}
