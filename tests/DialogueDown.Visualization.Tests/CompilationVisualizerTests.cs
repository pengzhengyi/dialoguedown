using System.Text.Json.Nodes;
using DialogueDown.Common;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Graph;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Regions;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Playbook;
using DialogueDown.Visualization.Render;
using NSubstitute;
using static DialogueDown.TestSupport.JsonAssert;
using static DialogueDown.TestSupport.ReportPayload;

namespace DialogueDown.Visualization.Tests;

public sealed class CompilationVisualizerTests
{
    [Fact]
    public void BuildStages_ErroringScriptThatReachedAnalysis_StillShowsEveryStageItReached()
    {
        // A jump to a missing scene is reported after the transpiler, so the compile runs every
        // stage and fails. A stage it reached still shows what it produced.
        var stages = new CompilationVisualizer(ScriptCompilerFactory.CreateDefault())
            .BuildStages("Alice: away => [nowhere](#no-such-scene)");

        Assert.Equal(5, stages.Count);
        Assert.All(stages.Take(4), stage => Assert.Null(stage.Unavailable));

        // The graph is the exception: it is built only for a script that compiled cleanly.
        Assert.Equal("Dialogue Graph", stages[4].Title);
        Assert.NotNull(stages[4].Unavailable);
    }

    [Fact]
    public void Constructor_NullCompiler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompilationVisualizer((IScriptCompiler)null!));
    }

    [Fact]
    public void Constructor_NullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompilationVisualizer((AppliedConfiguration)null!));
    }

    [Fact]
    public void RenderHtmlReport_WithConfigurationFile_EmbedsItsPathSourceAndSpeakers()
    {
        var toml = """
            [[speakers]]
            name = "Narrator"
            """;
        var applied = AppliedConfiguration.FromFile(
            "/proj/dialogue.toml",
            toml,
            new CompilerOptions { Speakers = [new ConfiguredSpeaker("Narrator", null, [], [])] });

        var report = FromPage(new CompilationVisualizer(applied).RenderHtmlReport("The room is quiet."));

        Assert.Equal("/proj/dialogue.toml", report.ConfigPath);
        Assert.Equal(toml, report.ConfigSource);
        Assert.Equal(["Narrator"], report.SpeakerNames);
    }

    [Fact]
    public void RenderHtmlReport_EmbedsThePlaybookTheCompileProduced()
    {
        var html = new CompilationVisualizer().RenderHtmlReport(
            """
            # Scene

            Narrator: The room is quiet.

            """,
            "/proj/quiet.dialogue.md");

        var playbook = FromPage(html).Playbook;
        // The playbook names the script it was compiled from, not the path the report was written to.
        Assert.Equal("quiet.dialogue.md", (string?)playbook?["metadata"]?["script"]);
        Assert.Equal(["Narrator"], SpeakerNamesIn(playbook));
    }

    [Fact]
    public void RenderHtmlReport_HaltedCompile_EmbedsThePlaybooksUnavailableReason()
    {
        var html = new CompilationVisualizer().RenderHtmlReport("=> [Gone](#missing)\n");

        Assert.Equal(PlaybookProjection.UnavailableReason, (string?)FromPage(html).Playbook?["unavailable"]);
    }

    [Fact]
    public void SerializeDocument_EmbedsThePlaybookForTheLiveReport()
    {
        var json = new CompilationVisualizer().SerializeDocument(
            "/proj/quiet.dialogue.md",
            """
            # Scene

            Narrator: Quiet.

            """,
            "edit");

        Assert.Equal("quiet.dialogue.md", (string?)Parse(json).Playbook?["metadata"]?["script"]);
    }

    [Fact]
    public void RenderHtmlReport_WithoutConfigurationContext_OmitsTheConfigurationField()
    {
        var html = new CompilationVisualizer().RenderHtmlReport("The room is quiet.");

        AssertOmits(FromPage(html).Json, "configuration");
    }

    [Fact]
    public void Constructor_WithOptions_IncludesConfiguredSpeakersInTheReport()
    {
        var options = new CompilerOptions
        {
            Speakers = [new ConfiguredSpeaker("Narrator", null, [], [])],
        };

        var html = new CompilationVisualizer(options).RenderHtmlReport("The room is quiet.");

        AssertJson("""["Narrator"]""", FromPage(html).Symbols?["speakers"]);
    }

    [Fact]
    public void BuildStages_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompilationVisualizer().BuildStages(null!));
    }

    [Fact]
    public void BuildStages_ProjectsTheStagesFromTheCompilerSeam()
    {
        var markdown = new MarkdownDocument(
        [
            new Paragraph([new TextInline("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var script = new ScriptDocument(
        [
            new Line(null, [new Text("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var compiler = Substitute.For<IScriptCompiler>();
        var desugared = new DesugaredScriptDocument(script);
        var semantics = new SemanticAnalyzer(new SemanticAnalyzerOptions([]))
            .Analyze(desugared, new DiagnosticsContext("script source", new DiagnosticBag()));
        compiler.Compile("script source").Returns(
            new CompilationSuccess(
                "script source",
                markdown,
                script,
                desugared,
                semantics,
                // The hand-built AST lacks the defaults lowering needs, so the graph is an empty
                // stand-in.
                EmptyGraph(),
                []));
        var visualizer = new CompilationVisualizer(compiler);

        var stages = visualizer.BuildStages("script source");

        compiler.Received(1).Compile("script source");
        Assert.Collection(
            stages,
            markdownStage => Assert.Equal("Markdown AST", markdownStage.Title),
            dialogueStage => Assert.Equal("Dialogue AST", dialogueStage.Title),
            desugaredStage => Assert.Equal("Desugared AST", desugaredStage.Title),
            semanticStage => Assert.Equal("Semantic Model", semanticStage.Title),
            graphStage => Assert.Equal("Dialogue Graph", graphStage.Title));
        Assert.Contains(stages[0].Nodes, n => n.Label == "Paragraph");
        Assert.Contains(stages[1].Nodes, n => n.Label == "Line");
        Assert.Contains(stages[2].Nodes, n => n.Label == "Line");
        Assert.NotNull(stages[3].Tables);

        // The Markdown AST has not read Dialogue meaning, so `=>` there is still plain text; every
        // stage from the transpiler on has.
        Assert.False(stages[0].ReadsDialogueMeaning);
        Assert.All(stages.Skip(1), stage => Assert.True(stage.ReadsDialogueMeaning));
    }

    [Fact]
    public void BuildStages_HaltedResult_KeepsProducedStagesAndDisablesTheRest()
    {
        var markdown = new MarkdownDocument(
        [
            new Paragraph([new TextInline("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var script = new ScriptDocument(
        [
            new Line(null, [new Text("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var compiler = Substitute.For<IScriptCompiler>();
        // A halted compile: the transpiler produced the Dialogue AST, but desugar and semantic
        // analysis never ran, so their stages are unavailable.
        compiler.Compile("broken").Returns(
            CompilationFailure.AtTranspile("broken", markdown, script, []));

        var stages = new CompilationVisualizer(compiler).BuildStages("broken");

        Assert.Collection(
            stages,
            markdownStage =>
            {
                Assert.Equal("Markdown AST", markdownStage.Title);
                Assert.Null(markdownStage.Unavailable);
            },
            dialogueStage =>
            {
                Assert.Equal("Dialogue AST", dialogueStage.Title);
                Assert.Null(dialogueStage.Unavailable);
            },
            desugaredStage =>
            {
                Assert.Equal("Desugared AST", desugaredStage.Title);
                Assert.NotNull(desugaredStage.Unavailable);
                Assert.Empty(desugaredStage.Nodes);
            },
            semanticStage =>
            {
                Assert.Equal("Semantic Model", semanticStage.Title);
                Assert.NotNull(semanticStage.Unavailable);
                Assert.Empty(semanticStage.Nodes);
            },
            graphStage =>
            {
                Assert.Equal("Dialogue Graph", graphStage.Title);
                Assert.NotNull(graphStage.Unavailable);
                Assert.Empty(graphStage.Nodes);
            });
    }

    [Fact]
    public void RenderHtmlReport_HaltedResult_HasEmptyEditorSymbols()
    {
        var markdown = new MarkdownDocument(
        [
            new Paragraph([new TextInline("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var script = new ScriptDocument(
        [
            new Line(null, [new Text("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);
        var compiler = Substitute.For<IScriptCompiler>();
        compiler.Compile("broken").Returns(
            CompilationFailure.AtTranspile("broken", markdown, script, []));

        var html = new CompilationVisualizer(compiler).RenderHtmlReport("broken");

        AssertJson(
            """
            {
              "jumpTargets": [{ "slug": "END", "heading": "End the run" }],
              "speakers": [],
              "speakerIds": [],
              "tags": [],
              "reservedTargets": [{ "anchor": "END", "label": "End", "role": "Terminal" }]
            }
            """,
            FromPage(html).Symbols);
    }

    [Fact]
    public void BuildStages_RealCompiler_DesugaredStageFillsADefaultSpeakerOnASpeakerlessLine()
    {
        var stages = new CompilationVisualizer().BuildStages("The room is quiet.");

        var desugared = stages[2];
        Assert.Equal("Desugared AST", desugared.Title);
        Assert.False(string.IsNullOrWhiteSpace(desugared.Description));
        // The speaker-less line has no speaker in the Dialogue AST, but the desugarer
        // fills a synthetic default speaker, so only the Desugared stage shows it.
        Assert.DoesNotContain(stages[1].Nodes, n => n.Label == "Speaker (default)");
        Assert.Contains(desugared.Nodes, n => n.Label == "Speaker (default)");
    }

    [Fact]
    public void BuildStages_RealCompiler_ATranspileErrorHaltsAndDisablesTheLaterStages()
    {
        // "#lonely: Hi" is a tags-without-speaker error reported during transpile: the
        // stage-boundary compile halts, so the desugared and semantic stages are never produced
        // and render as disabled tabs.
        var stages = new CompilationVisualizer().BuildStages("#lonely: Hi");

        Assert.Equal(5, stages.Count);
        Assert.Null(stages[0].Unavailable); // Markdown AST — produced
        Assert.Null(stages[1].Unavailable); // Dialogue AST — produced
        Assert.Equal("Desugared AST", stages[2].Title);
        Assert.NotNull(stages[2].Unavailable); // disabled
        Assert.Empty(stages[2].Nodes);
        Assert.Equal("Semantic Model", stages[3].Title);
        Assert.NotNull(stages[3].Unavailable); // disabled
        Assert.Equal("Dialogue Graph", stages[4].Title);
        Assert.NotNull(stages[4].Unavailable); // disabled
    }

    [Fact]
    public void BuildStages_RealCompiler_ProducesDialogueStageWithSpeakersChoicesAndCalls()
    {
        var stages = new CompilationVisualizer().BuildStages(
            """
            # Scene

            Alice: Hello, **there**! `Wave()`

            - Go left
            - Go right
            """);

        var dialogue = Assert.IsType<DisplayGraph>(stages[1]);
        Assert.Equal("Dialogue AST", dialogue.Title);
        Assert.False(string.IsNullOrWhiteSpace(dialogue.Description));
        Assert.Contains(dialogue.Nodes, n => n.Label == "Line");
        Assert.Contains(dialogue.Nodes, n => n.Label.StartsWith("Speaker", StringComparison.Ordinal));
        Assert.Contains(dialogue.Nodes, n => n.Label.StartsWith("Choices", StringComparison.Ordinal));
        Assert.Contains(dialogue.Nodes, n => n.Category == "call");     // the `Wave()` game call
        Assert.Contains(dialogue.Nodes, n => n.Category == "styling");  // the **there** bold
    }

    [Fact]
    public void RenderHtmlReport_RealCompiler_ProducesSelfContainedReportWithStageAndLabels()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport(
            """
            # Hello

            World
            """);

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cdn.jsdelivr.net", html);    // self-contained: no CDN
        Assert.Contains(".tippy-box", html);                // client libs inlined
        var report = FromPage(html);
        var markdown = report.Stages![0]!;
        Assert.Equal("Markdown AST", (string?)markdown["title"]);
        var heading = Assert.Single(NodesIn(markdown), node => (string?)node["label"] == "Heading (H1)");
        Assert.Equal("# Hello", (string?)heading["source"]);    // the heading's own source
        Assert.Contains(NodesIn(markdown), node => (string?)node["label"] == "Paragraph");
        Assert.Equal("# Hello\n\nWorld", report.Source);      // the whole document, for the Source tab
    }

    [Fact]
    public void RenderLiveReport_MarksThePayloadWithTheModeAndDocumentPath()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderLiveReport("scene.dialogue.md", "# Hello", "view");

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        var report = FromPage(html);
        Assert.Equal("view", report.Mode);
        Assert.Equal("scene.dialogue.md", report.Path);
        Assert.Equal("Markdown AST", (string?)report.Stages![0]!["title"]);
    }

    [Fact]
    public void RenderEmptyShell_CarriesTheProjectWithNoActiveDocument()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderEmptyShell("/project", "edit");

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        var report = FromPage(html);
        Assert.Equal("edit", report.Mode);
        // The project it serves, and no active document: no active path, no source, no stages to show.
        AssertJson("""{ "root": "/project" }""", report.Json["project"]);
        AssertOmits(report.Json, "source");
        Assert.Empty(report.Stages!);
    }

    [Fact]
    public void RenderHtmlReport_MarksThePayloadStatic()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport("# Hello");

        Assert.Equal("static", FromPage(html).Mode);
    }

    [Fact]
    public void RenderHtmlReport_WithDocumentPath_IncludesThePathButStaysStatic()
    {
        var visualizer = new CompilationVisualizer();

        var report = FromPage(visualizer.RenderHtmlReport("# Hello", "scene.dialogue.md"));

        Assert.Equal("static", report.Mode);
        Assert.Equal("scene.dialogue.md", report.Path);
    }

    [Fact]
    public void RenderLiveReport_NullDocumentPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CompilationVisualizer().RenderLiveReport(null!, "# Hello", "view"));
    }

    [Fact]
    public void SerializeDocument_ReturnsModePathSourceAndStages()
    {
        var visualizer = new CompilationVisualizer();

        var report = Parse(visualizer.SerializeDocument("scene.dialogue.md", "# Hello", "view"));

        Assert.Equal("view", report.Mode);
        Assert.Equal("scene.dialogue.md", report.Path);
        Assert.Equal("# Hello", report.Source);
        Assert.Equal("Markdown AST", (string?)report.Stages![0]!["title"]);
    }

    [Fact]
    public void SerializeDocument_NullDocumentPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CompilationVisualizer().SerializeDocument(null!, "# Hello", "view"));
    }

    [Fact]
    public void SerializeDocument_ForABrokenScript_CarriesTheLspDiagnostics()
    {
        var visualizer = new CompilationVisualizer();

        var json = visualizer.SerializeDocument(
            "scene.dialogue.md",
            """
            # Chapter
            Alice: Hello.

            # Chapter
            Bob: Goodbye.
            """,
            "view");

        var diagnostic = Assert.Single(Parse(json).Diagnostics!)!;
        Assert.Equal("DLG2001", (string?)diagnostic["code"]);
        Assert.Equal("dialoguedown", (string?)diagnostic["source"]);
    }

    [Fact]
    public void SerializeDocument_ForACleanScript_CarriesAnEmptyDiagnosticsArray()
    {
        var visualizer = new CompilationVisualizer();

        var json = visualizer.SerializeDocument(
            "scene.dialogue.md",
            """
            # Hello
            Alice: Hi.
            """,
            "view");

        AssertJson("[]", Parse(json).Diagnostics);
    }

    [Fact]
    public void SerializeDocument_CarriesTheSemanticTokensForHighlighting()
    {
        var visualizer = new CompilationVisualizer();

        var json = visualizer.SerializeDocument(
            "scene.dialogue.md",
            """
            # Hello
            Alice #happy: Hi. => #next
            """,
            "view");

        var kinds = Parse(json).SemanticTokens!.Select(token => (string?)token!["kind"]).ToList();
        Assert.Contains("SpeakerName", kinds);
        Assert.Contains("CustomTag", kinds);
        Assert.Contains("Separator", kinds);
        Assert.Contains("JumpIndicator", kinds);
        Assert.DoesNotContain("Speaker", kinds); // a speaker is SpeakerName
    }

    [Fact]
    public void RenderHtmlReport_ForADocumentWithNoDialogue_CarriesAnEmptySemanticTokensArray()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport("# Just a heading");

        AssertJson("[]", FromPage(html).SemanticTokens);
    }

    [Fact]
    public void LocalImageReferences_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CompilationVisualizer().LocalImageReferences(null!));
    }

    [Fact]
    public void LocalImageReferences_ReturnsImageSourcesInDocumentOrder()
    {
        var references = new CompilationVisualizer().LocalImageReferences(
            """
            Alice: This is *your* photo. ![Bob's photo](assets/bob.jpg)

            - Bob: And a painting. ![Painting](../shared/painting.png)
            """);

        Assert.Equal(["assets/bob.jpg", "../shared/painting.png"], references);
    }

    [Fact]
    public void LocalImageReferences_SkipsWebAndDataUrls()
    {
        var references = new CompilationVisualizer().LocalImageReferences(
            """
            ![remote](https://example.com/a.png)
            ![protocol](//example.com/b.png)
            ![data](data:image/png;base64,AAAA)
            ![local](assets/c.png)
            """);

        Assert.Equal(["assets/c.png"], references);
    }

    [Fact]
    public void LocalImageReferences_IncludesAbsoluteFilesystemPaths()
    {
        var references = new CompilationVisualizer().LocalImageReferences(
            "![outside](/var/gallery/painting.jpg)");

        Assert.Equal(["/var/gallery/painting.jpg"], references);
    }

    [Fact]
    public void LocalImageReferences_NoImages_ReturnsEmpty()
    {
        var references = new CompilationVisualizer().LocalImageReferences(
            """
            # Just a heading

            Alice: Hi.
            """);

        Assert.Empty(references);
    }

    [Fact]
    public void RenderText_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CompilationVisualizer().RenderText(null!, EmitFormat.Dot));
    }

    [Fact]
    public void RenderText_Dot_EmitsEveryStageUnderAHeaderAsDigraph()
    {
        var text = new CompilationVisualizer().RenderText(
            """
            # Scene

            Alice: Hi.
            """,
            EmitFormat.Dot);

        Assert.Contains("// Markdown AST", text);
        Assert.Contains("// Dialogue AST", text);
        Assert.Contains("// Desugared AST", text);
        Assert.Contains("digraph", text);
    }

    [Fact]
    public void RenderHtmlReport_InlinesTheClientSoAnExportedFileWorksOffline()
    {
        // An exported report is one file a reader can open or mail around; it must carry the
        // client with it. Serving is the only place that may link the client instead.
        var html = new CompilationVisualizer().RenderHtmlReport("The room is quiet.");

        Assert.Contains("--pico-", html, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"/assets/", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/assets/", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ServedPages_LinkTheClientSoABrowserReusesItAcrossDocuments(bool emptyShell)
    {
        var visualizer = new CompilationVisualizer();

        var html = emptyShell
            ? visualizer.RenderEmptyShell("/project", VisualizationMode.View)
            : visualizer.RenderLiveReport("scene.dialogue.md", "The room is quiet.", VisualizationMode.View);

        Assert.Contains("src=\"/assets/", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/assets/", html, StringComparison.Ordinal);
        Assert.DoesNotContain("--pico-", html, StringComparison.Ordinal);
        // The payload stays inline; only the client is linked.
        Assert.Contains("__DD_REPORT__", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderHtmlReport_LeavesMermaidOutOfAScriptThatDrawsNoDiagram()
    {
        // Mermaid's build is larger than the rest of the report together, and almost no script
        // needs it, so an export only carries it when the script actually asks for a diagram.
        var html = new CompilationVisualizer().RenderHtmlReport("The room is quiet.");

        Assert.DoesNotContain("__esbuild_esm_mermaid_nm", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderHtmlReport_CarriesMermaidForAScriptThatDrawsOne()
    {
        var source = """
            The room is quiet.

            ```mermaid
            flowchart LR
              a --> b
            ```

            """;

        var html = new CompilationVisualizer().RenderHtmlReport(source);

        // Its own build assigns the global the report reads, so the diagram draws with no network.
        Assert.Contains("__esbuild_esm_mermaid_nm", html, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"/assets/", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ServedPages_NameWhereMermaidLivesRatherThanCarryingIt()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderLiveReport(
            "scene.dialogue.md", "The room is quiet.", VisualizationMode.View);

        Assert.DoesNotContain("__esbuild_esm_mermaid_nm", html, StringComparison.Ordinal);
        Assert.Contains("__DD_MERMAID__ = \"/assets/", html, StringComparison.Ordinal);
    }

    // The nodes a serialized stage shows.
    private static IEnumerable<JsonNode> NodesIn(JsonNode stage) =>
        stage["nodes"]!.AsArray().Select(node => node!);

    // The names of the speakers a serialized playbook section declares.
    private static IReadOnlyList<string?> SpeakerNamesIn(JsonNode? playbook) =>
        [.. playbook?["speakers"]?.AsArray().Select(speaker => (string?)speaker?["name"]) ?? []];

    private static DialogueGraph EmptyGraph()
    {
        var end = new NodeId(0);
        return new DialogueGraph(
            [new EndNode(end, new SourceSpan(0, 0))], entry: end, end: end, RegionTree.Empty);
    }
}
