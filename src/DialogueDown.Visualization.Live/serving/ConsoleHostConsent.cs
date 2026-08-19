namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// Asks for hosting consent on the console. When there is no interactive terminal
/// (piped input, CI), it declines and points at <c>--render-root</c> instead of
/// blocking on a prompt no one can answer.
/// </summary>
internal sealed class ConsoleHostConsent : IHostConsent
{
    private readonly bool _interactive;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    /// <summary>
    /// Creates a console prompt. <paramref name="interactive"/> is normally
    /// <c>!Console.IsInputRedirected</c>; when false the prompt is skipped and
    /// hosting is declined.
    /// </summary>
    public ConsoleHostConsent(bool interactive, TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        _interactive = interactive;
        _input = input;
        _output = output;
    }

    /// <inheritdoc />
    public bool AllowHosting(HostConsentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _output.WriteLine(
            $"This document references {request.OutsideImages.Count} image(s) outside its folder:");
        foreach (var image in request.OutsideImages)
        {
            _output.WriteLine($"  {image}");
        }

        if (!_interactive)
        {
            _output.WriteLine(
                $"Not hosting them (no interactive prompt). Re-run with --render-root \"{request.RootDirectory}\" to allow.");
            return false;
        }

        _output.Write($"Allow serving files from {request.RootDirectory} to render them? [y/N] ");
        var answer = _input.ReadLine()?.Trim();
        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
