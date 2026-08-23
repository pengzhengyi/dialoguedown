using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Visualization.Playbook;

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
        Assert.Contains("\"nodes\"", report.Json, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_ACleanCompile_FormatsTheJsonForReading()
    {
        // The report shows the playbook to be read, not diffed against a file, so it is indented.
        var report = Project(Source);

        Assert.Contains("\n  ", report.Json!, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_ACleanCompile_SummarizesTheHeaderFacts()
    {
        var report = Project(Source);

        var metadata = Assert.IsType<PlaybookMetadataView>(report.Metadata);
        Assert.Equal(Script, metadata.Script);
        // Every playbook needs the core capability; the format version is still pre-1.0, so the
        // number itself is not asserted — only that the summary reports what the document says.
        Assert.Contains("core", metadata.Requires);
        Assert.Equal(0, metadata.Entry);
        Assert.True(metadata.NodeCount > 0, "A compiled script has nodes to play.");
        Assert.True(metadata.AnchorCount > 0, "The script has a heading, so it has an anchor.");
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
        var report = Project("# Scene\n\nAlice: Go => [Nowhere](#missing)\n");

        Assert.Null(report.Json);
        Assert.Null(report.Metadata);
        Assert.Empty(report.Speakers);
        Assert.Equal(PlaybookProjection.UnavailableReason, report.Unavailable);
    }

    private static PlaybookReport Project(string source) =>
        PlaybookProjection.Project(
            Compile(source), Script, PlaybookWriterFactory.CreateDefault());

    private static CompilationResult Compile(string source) =>
        ScriptCompilerFactory.CreateDefault().Compile(source);
}
