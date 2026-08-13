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
using NSubstitute;

namespace DialogueDown.Visualization.Tests;

public sealed class CompilationVisualizerTests
{
    [Fact]
    public void BuildStages_ErroringScriptThatReachedAnalysis_StillShowsEveryStageItReached()
    {
        // A jump to a missing scene is reported after the transpiler, so the compile runs every
        // stage and fails. Reaching a stage is not succeeding: the artifacts it produced are still
        // worth inspecting, so every stage before the graph reads as available.
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
        var applied = AppliedConfiguration.FromFile(
            "/proj/dialogue.toml",
            "[[speakers]]\nname = \"Narrator\"",
            new CompilerOptions { Speakers = [new ConfiguredSpeaker("Narrator", null, [], [])] });

        var html = new CompilationVisualizer(applied).RenderHtmlReport("The room is quiet.");

        Assert.Contains("\"configuration\"", html, StringComparison.Ordinal);
        Assert.Contains("dialogue.toml", html, StringComparison.Ordinal);
        Assert.Contains("Narrator", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderHtmlReport_WithoutConfigurationContext_OmitsTheConfigurationField()
    {
        var html = new CompilationVisualizer().RenderHtmlReport("The room is quiet.");

        Assert.DoesNotContain("\"configuration\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WithOptions_IncludesConfiguredSpeakersInTheReport()
    {
        var options = new CompilerOptions
        {
            Speakers = [new ConfiguredSpeaker("Narrator", null, [], [])],
        };

        var html = new CompilationVisualizer(options).RenderHtmlReport("The room is quiet.");

        Assert.Contains("Narrator", html, StringComparison.Ordinal);
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
                // This AST is assembled here rather than desugared, so it carries none of the
                // defaults lowering relies on; the visualizer projects no graph stage anyway.
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

        Assert.Contains(
            "\"symbols\":{\"jumpTargets\":[{\"slug\":\"END\",\"heading\":\"End the run\"}],"
            + "\"speakers\":[],\"speakerIds\":[],\"tags\":[],"
            + "\"reservedTargets\":[{\"anchor\":\"END\",\"label\":\"End\",\"role\":\"Terminal\"}]}",
            html,
            StringComparison.Ordinal);
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
        Assert.Contains("\"title\":\"Markdown AST\"", html);
        Assert.Contains("Heading (H1)", html);
        Assert.Contains("Paragraph", html);
        Assert.Contains("\"source\":\"# Hello\"", html);     // heading's source snippet
        Assert.Contains("\"source\":\"# Hello\\n\\nWorld\"", html); // whole document for the Source tab
    }

    [Fact]
    public void RenderLiveReport_MarksThePayloadWithTheModeAndDocumentPath()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderLiveReport("scene.dialogue.md", "# Hello", "view");

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"view\"", html);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", html);
        Assert.Contains("\"title\":\"Markdown AST\"", html);
    }

    [Fact]
    public void RenderEmptyShell_CarriesTheProjectWithNoActiveDocument()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderEmptyShell("/project", "edit");

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"project\":{", html);
        Assert.Contains("\"root\":\"/project\"", html);
        Assert.Contains("\"mode\":\"edit\"", html);
        // No active document: no active path, no source, no stages to show.
        Assert.DoesNotContain("\"activePath\"", html);
        Assert.DoesNotContain("\"source\":", html);
    }

    [Fact]
    public void RenderHtmlReport_MarksThePayloadStatic()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport("# Hello");

        Assert.Contains("\"mode\":\"static\"", html);
        Assert.DoesNotContain("\"mode\":\"view\"", html);
    }

    [Fact]
    public void RenderHtmlReport_WithDocumentPath_IncludesThePathButStaysStatic()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport("# Hello", "scene.dialogue.md");

        Assert.Contains("\"mode\":\"static\"", html);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", html);
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

        var json = visualizer.SerializeDocument("scene.dialogue.md", "# Hello", "view");

        Assert.Contains("\"mode\":\"view\"", json);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"source\":\"# Hello\"", json);
        Assert.Contains("\"stages\":[", json);
        Assert.Contains("\"title\":\"Markdown AST\"", json);
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

        Assert.Contains("\"diagnostics\":[", json);
        Assert.Contains("\"code\":\"DLG2001\"", json);
        Assert.Contains("\"source\":\"dialoguedown\"", json);
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

        Assert.Contains("\"diagnostics\":[]", json);
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

        Assert.Contains("\"semanticTokens\":[", json);
        Assert.Contains("\"kind\":\"SpeakerName\"", json);
        Assert.Contains("\"kind\":\"CustomTag\"", json);
        Assert.Contains("\"kind\":\"Separator\"", json);
        Assert.Contains("\"kind\":\"JumpIndicator\"", json);
        Assert.DoesNotContain("\"kind\":\"Speaker\"", json); // the coarse kind is retired
    }

    [Fact]
    public void RenderHtmlReport_ForADocumentWithNoDialogue_CarriesAnEmptySemanticTokensArray()
    {
        var visualizer = new CompilationVisualizer();

        var html = visualizer.RenderHtmlReport("# Just a heading");

        Assert.Contains("\"semanticTokens\":[]", html);
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
        var references = new CompilationVisualizer().LocalImageReferences("# Just a heading\n\nAlice: Hi.");

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
        var text = new CompilationVisualizer().RenderText("# Scene\n\nAlice: Hi.", EmitFormat.Dot);

        Assert.Contains("// Markdown AST", text);
        Assert.Contains("// Dialogue AST", text);
        Assert.Contains("// Desugared AST", text);
        Assert.Contains("digraph", text);
    }

    private static DialogueGraph EmptyGraph()
    {
        var end = new NodeId(0);
        return new DialogueGraph(
            [new EndNode(end, new SourceSpan(0, 0))], entry: end, end: end, RegionTree.Empty);
    }
}
