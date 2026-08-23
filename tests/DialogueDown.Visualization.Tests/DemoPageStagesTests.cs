using System.Text.RegularExpressions;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests;

/// <summary>
/// The demo landing page advertises a list of compiler stages by hand, while the report renders
/// whichever stages the visualization projects. This gate keeps the two honest: a stage added to
/// the pipeline shows up in every report automatically, so without this the page quietly goes
/// stale — which is exactly what happened when the Dialogue Graph stage shipped.
/// </summary>
public sealed partial class DemoPageStagesTests
{
    // Always present as the editor tab, and never a projected stage, so the page lists it but the
    // visualizer does not produce it.
    private const string EditorTab = "Source";

    [Fact]
    public void TheDemoPage_AdvertisesEveryStageTheReportRenders()
    {
        var rendered = RenderedStages();
        var advertised = AdvertisedStages();

        var missing = rendered.Except(advertised, StringComparer.Ordinal).Order(StringComparer.Ordinal);
        Assert.True(
            !missing.Any(),
            $"docs/demo/index.html does not advertise: {string.Join(", ", missing)}. "
                + "Add each to its stage list, with a sentence saying what the stage shows.");
    }

    [Fact]
    public void TheDemoPage_AdvertisesNoStageTheReportDoesNotRender()
    {
        var rendered = RenderedStages();
        var advertised = AdvertisedStages();

        var extra = advertised.Except(rendered, StringComparer.Ordinal).Order(StringComparer.Ordinal);
        Assert.True(
            !extra.Any(),
            $"docs/demo/index.html advertises stages the report no longer renders: "
                + $"{string.Join(", ", extra)}. Remove them, or restore the stage.");
    }

    /// <summary>The stage titles the visualization projects for a script that reaches every stage.</summary>
    private static IReadOnlyCollection<string> RenderedStages() =>
        [.. new CompilationVisualizer()
            .BuildStages(ExampleScripts.Read("gallery.dialogue.md"))
            .Select(stage => stage.Title)];

    /// <summary>
    /// The stage names the landing page shows, which it marks up as bold list entries. The editor
    /// tab is dropped: the page lists it for the reader, but no stage projects it.
    /// </summary>
    private static IReadOnlyCollection<string> AdvertisedStages()
    {
        var page = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "demo", "index.html"));
        return [.. StageEntry().Matches(page)
            .Select(match => match.Groups[1].Value.Trim())
            .Where(name => name != EditorTab)];
    }

    [GeneratedRegex("<strong>([^<]+)</strong>")]
    private static partial Regex StageEntry();
}
