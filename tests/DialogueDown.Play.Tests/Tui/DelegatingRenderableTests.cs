using DialogueDown.Play.Tests.Support;
using DialogueDown.Play.Tui;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DialogueDown.Play.Tests.Tui;

/// <summary>
/// The renderable a live frame redraws through: it asks for its content again on every render, so a
/// redraw shows the current state rather than the frame the display started with.
/// </summary>
public sealed class DelegatingRenderableTests
{
    [Fact]
    public void MeasureAndRender_AskTheirContentEachTime()
    {
        var asked = 0;
        var renderable = new DelegatingRenderable(() =>
        {
            asked++;
            return new Text("x");
        });
        var console = Consoles.Plain();
        var options = new RenderOptions(
            console.Profile.Capabilities, new Size(console.Profile.Width, console.Profile.Height));

        renderable.Measure(options, console.Profile.Width);
        _ = renderable.Render(options, console.Profile.Width).ToList();

        Assert.Equal(2, asked);
    }
}
