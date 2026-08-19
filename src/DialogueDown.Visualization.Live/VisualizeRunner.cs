using DialogueDown.Configuration;
using DialogueDown.Visualization.Configuration;

using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// The default <see cref="IVisualizeRunner"/>: hides the export wiring behind the static HTML
/// export and the text emit, opening results with the injected browser launcher.
/// </summary>
public sealed class VisualizeRunner : IVisualizeRunner
{
    private readonly IBrowserLauncher _browser;

    public VisualizeRunner(IBrowserLauncher browser)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <inheritdoc />
    public int RunStatic(string file, string? output, bool noOpen, AppliedConfiguration configuration) =>
        StaticMode.Run(file, output, noOpen, configuration, _browser, Console.Error);

    /// <inheritdoc />
    public int RunEmit(string file, EmitFormat format, string? output, CompilerOptions options) =>
        EmitMode.Run(file, format, output, options, Console.Out, Console.Error);
}
