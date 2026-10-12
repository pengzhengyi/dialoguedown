using Spectre.Console.Rendering;

namespace DialogueDown.Play.Tui;

/// <summary>
/// A renderable whose content is asked for on every render, so a live display redraws whatever the
/// current state is.
/// </summary>
internal sealed class DelegatingRenderable(Func<IRenderable> content) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        content().Measure(options, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        content().Render(options, maxWidth);
}
