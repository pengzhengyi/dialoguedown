using System.Text.Json;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Visualization.Playbook;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests.Playbook;

public sealed class PlaybookProjectionTests
{
    private const string Script = "scene.dialogue.md";

    private const string Source = """
        # The Crossroads

        Alice: Which way?

        - Left => [North](#the-crossroads)
        - Right
        """;

    [Fact]
    public void Project_ACleanCompile_CarriesThePlaybookAsJson()
    {
        var report = Project(Source);

        Assert.NotNull(report.Json);
        Assert.Null(report.Unavailable);
        var playbook = JsonSerializer.Deserialize<PlaybookDocument>(report.Json, PlaybookJson.Options);
        Assert.NotNull(playbook);
        Assert.Equal(Script, playbook.Script);
        Assert.NotEmpty(playbook.Nodes);
    }

    [Fact]
    public void Project_ACleanCompile_FormatsTheJsonForReading()
    {
        // The report shows the playbook to be read, not diffed against a file, so it is indented.
        var report = Project(Source);

        Assert.NotNull(report.Json);
        Assert.Contains("\n  ", report.Json, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_ACleanCompile_SummarizesTheHeaderFacts()
    {
        var report = Project(Source);

        var metadata = Assert.IsType<PlaybookMetadataView>(report.Metadata);
        Assert.Equal(Script, metadata.Script);
        // Every playbook needs the core capability. The format version changes between releases,
        // so it is not asserted.
        Assert.Contains("core", metadata.Requires);
        Assert.Equal(0, metadata.Entry);
        // Alice's line, the menu, the line each option echoes, and the end.
        Assert.Equal(5, metadata.NodeCount);
        Assert.Equal(1, metadata.AnchorCount);
    }

    [Fact]
    public void Project_ACleanCompile_ListsTheSpeakersThePlaybookDeclares()
    {
        var report = Project(Source);

        Assert.Contains(report.Speakers, speaker => speaker.Name == "Alice");
    }

    [Fact]
    public void Project_AScriptWithErrors_HasNoPlaybookAndSaysWhy()
    {
        // The graph is built only for a clean compile, and only a graph becomes a playbook.
        var report = Project(
            """
            # Scene

            Alice: Go => [Nowhere](#missing)

            """);

        Assert.Null(report.Json);
        Assert.Null(report.Metadata);
        Assert.Empty(report.Speakers);
        Assert.Equal(PlaybookProjection.UnavailableReason, report.Unavailable);
    }

    [Fact]
    public void Project_ListsEveryAnchorWithTheNodeItLandsOn()
    {
        var report = Project(
            """
            # The Tavern

            Alice: Hello.

            # The Road

            Alice: Onward.

            """);

        Assert.Collection(
            report.Anchors.OrderBy(anchor => anchor.Name, StringComparer.Ordinal),
            first => Assert.Equal("the-road", first.Name),
            second => Assert.Equal("the-tavern", second.Name));
        Assert.All(report.Anchors, anchor => Assert.True(anchor.Node >= 0));
    }

    [Fact]
    public void Project_HaltedCompile_HasNoAnchors()
    {
        Assert.Empty(Project("=> [Gone](#missing)\n").Anchors);
    }

    private static PlaybookReport Project(string source) =>
        PlaybookProjection.Project(
            Pipeline.Compilation(source), Script, PlaybookWriterFactory.CreateDefault());
}
