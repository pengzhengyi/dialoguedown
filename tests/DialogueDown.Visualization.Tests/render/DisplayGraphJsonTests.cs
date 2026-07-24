using DialogueDown.Visualization.Diagnostics;
using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Lsp;
using static DialogueDown.Visualization.Tests.Support.Display;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class DisplayGraphJsonTests
{
    [Fact]
    public void Serialize_UsesCamelCaseKeysAndStringEnum()
    {
        var graph = MakeGraph(
            "Markdown AST",
            [Node("n0", "Document"), Node("n1", "Heading", Attr("level", "2"))],
            [Child("n0", "n1")]);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"title\":\"Markdown AST\"", json);
        Assert.Contains("\"nodes\":", json);
        Assert.Contains("\"label\":\"Heading\"", json);
        Assert.Contains("\"attributes\":[{\"name\":\"level\",\"value\":\"2\"}]", json);
        Assert.Contains("\"fromId\":\"n0\"", json);
        Assert.Contains("\"kind\":\"Child\"", json);
    }

    [Fact]
    public void Serialize_IncludesStageDescription()
    {
        var graph = MakeGraph(
            "Markdown AST", [Node("n0", "Document")], [], description: "What it shows.");

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"description\":\"What it shows.\"", json);
    }

    [Fact]
    public void Serialize_IncludesUnavailableReasonForADisabledStage()
    {
        var graph = DisplayGraph.ForUnavailableStage(
            "Semantic Model", "What it would show.", "Unavailable due to compilation errors.");

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"unavailable\":{\"reason\":\"Unavailable due to compilation errors.\"}", json);
        Assert.Contains("\"nodes\":[]", json);
    }

    [Fact]
    public void Serialize_OmitsUnavailableForAProducedStage()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.DoesNotContain("unavailable", json);
    }

    [Fact]
    public void Serialize_IncludesNodeSourceWhenPresentAndOmitsWhenNull()
    {
        var graph = MakeGraph(
            "G",
            [new DisplayNode("n0", "Text", [], "# Hi"), Node("n1", "Empty")],
            []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"source\":\"# Hi\"", json);
        var sourceKeyCount = json.Split("\"source\":").Length - 1;
        Assert.Equal(1, sourceKeyCount);
    }

    [Fact]
    public void Serialize_IncludesNodeCategoryWhenPresent()
    {
        var graph = MakeGraph("G", [new DisplayNode("n0", "Code span", [], null, "call")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"category\":\"call\"", json);
    }

    [Fact]
    public void Serialize_IncludesNodeEntityKeyWhenPresentAndOmitsWhenNull()
    {
        var graph = MakeGraph(
            "G",
            [new DisplayNode("n0", "The Market", [], null, "structure", "scene:the-market"), Node("n1", "Plain")],
            []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"entityKey\":\"scene:the-market\"", json);
        var entityKeyCount = json.Split("\"entityKey\":").Length - 1;
        Assert.Equal(1, entityKeyCount);
    }

    [Fact]
    public void Serialize_OmitsTablesWhenNullAndIncludesThemWhenPresent()
    {
        var plain = MakeGraph("G", [Node("n0", "Document")], []);
        Assert.DoesNotContain("\"tables\":", DisplayGraphJson.Serialize([plain]));

        var withTables = plain with
        {
            Tables =
            [
                new SemanticTable(
                    "Anchors",
                    ["Anchor", "Scene"],
                    [new SemanticRow([new SemanticCell("#the-market"), new SemanticCell("The Market")], "scene:the-market")],
                    "No scenes."),
            ],
        };
        var json = DisplayGraphJson.Serialize([withTables]);

        Assert.Contains("\"tables\":[", json);
        Assert.Contains("\"title\":\"Anchors\"", json);
        Assert.Contains("\"columns\":[\"Anchor\",\"Scene\"]", json);
        Assert.Contains("\"entityKey\":\"scene:the-market\"", json);
        Assert.Contains("\"emptyText\":\"No scenes.\"", json);
    }

    [Fact]
    public void Serialize_IncludesCellRefKeyForACrossLink()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []) with
        {
            Tables =
            [
                new SemanticTable(
                    "Jump resolutions",
                    ["Resolves to"],
                    [new SemanticRow([new SemanticCell("\u2192 The Market", RefKey: "scene:the-market")])],
                    "No jumps."),
            ],
        };

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.Contains("\"refKey\":\"scene:the-market\"", json);
    }

    [Fact]
    public void Serialize_EscapesHtmlSensitiveCharacters_SoScriptCannotBreakOut()
    {
        var graph = MakeGraph("G", [Node("n0", "</script><b>&")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.DoesNotContain("</script>", json);
        Assert.Contains("\\u003C", json); // '<' is unicode-escaped
    }

    [Fact]
    public void SerializeReport_WrapsModeSourceAndStages()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hello", [graph]);

        Assert.Contains("\"mode\":\"static\"", json);
        Assert.Contains("\"source\":\"# Hello\"", json);
        Assert.Contains("\"stages\":[", json);
        Assert.Contains("\"title\":\"Markdown AST\"", json);
    }

    [Fact]
    public void SerializeReport_OmitsSourceWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, null, [graph]);

        Assert.DoesNotContain("\"source\":", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void SerializeReport_WithModeAndPath_AddsBoth()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("view", "scene.dialogue.md", "# Hi", [graph]);

        Assert.Contains("\"mode\":\"view\"", json);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"source\":\"# Hi\"", json);
    }

    [Fact]
    public void SerializeReport_WithoutPath_OmitsPath()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]);

        Assert.DoesNotContain("\"path\":", json);
    }

    [Fact]
    public void SerializeReport_OmitsSymbolsWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]);

        Assert.DoesNotContain("\"symbols\":", json);
    }

    [Fact]
    public void SerializeReport_IncludesSymbolsWhenPresent()
    {
        var graph = MakeGraph("Semantic Model", [Node("n0", "Document")], []);
        var symbols = new SymbolSet(
            [new JumpTargetSymbol("the-market", "The Market")], ["Guide"], ["guide"], ["wise"]);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph], symbols);

        Assert.Contains("\"symbols\":{", json);
        Assert.Contains("\"jumpTargets\":[{\"slug\":\"the-market\",\"heading\":\"The Market\"}]", json);
        Assert.Contains("\"speakers\":[\"Guide\"]", json);
        Assert.Contains("\"speakerIds\":[\"guide\"]", json);
        Assert.Contains("\"tags\":[\"wise\"]", json);
    }

    [Fact]
    public void SerializeReport_OmitsDiagnosticsWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]);

        Assert.DoesNotContain("\"diagnostics\":", json);
    }

    [Fact]
    public void SerializeReport_IncludesDiagnosticsWithTheLspShape()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var diagnostics = new List<LspDiagnostic>
        {
            new(
                new LspRange(new LspPosition(2, 0), new LspPosition(2, 8)),
                LspSeverity.Error,
                "DLG2001",
                "Two scenes resolve to the same anchor '#chapter'.",
                "dialoguedown"),
        };

        var json = DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: diagnostics);

        Assert.Contains(
            "\"range\":{\"start\":{\"line\":2,\"character\":0},\"end\":{\"line\":2,\"character\":8}}",
            json);
        Assert.Contains("\"code\":\"DLG2001\"", json);
        Assert.Contains("\"source\":\"dialoguedown\"", json);
    }

    [Fact]
    public void SerializeReport_WritesSeverityAsTheLspNumber()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var diagnostics = new List<LspDiagnostic>
        {
            new(
                new LspRange(new LspPosition(0, 0), new LspPosition(0, 1)),
                LspSeverity.Error, "DLG0001", "Boom.", "dialoguedown"),
        };

        var json = DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: diagnostics);

        Assert.Contains("\"severity\":1", json);
        Assert.DoesNotContain("\"severity\":\"Error\"", json);
    }

    [Fact]
    public void SerializeReport_WritesAnEmptyDiagnosticsArray_SoACleanCompileClearsTheOverlay()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: []);

        Assert.Contains("\"diagnostics\":[]", json);
    }

    [Fact]
    public void SerializeDocument_IncludesDiagnostics()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var diagnostics = new List<LspDiagnostic>
        {
            new(
                new LspRange(new LspPosition(1, 2), new LspPosition(1, 5)),
                LspSeverity.Warning, "DLG3001", "Suspect.", "dialoguedown"),
        };

        var json = DisplayGraphJson.SerializeDocument(
            "view", "scene.dialogue.md", "# Hi", [graph], diagnostics: diagnostics);

        Assert.Contains("\"code\":\"DLG3001\"", json);
        Assert.Contains("\"severity\":2", json);
    }

    [Fact]
    public void SerializeDocument_WrapsModePathSourceAndStages()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeDocument("view", "scene.dialogue.md", "# Hi", [graph]);

        Assert.Contains("\"mode\":\"view\"", json);
        Assert.Contains("\"path\":\"scene.dialogue.md\"", json);
        Assert.Contains("\"source\":\"# Hi\"", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void SerializeReport_OmitsSemanticTokensWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]);

        Assert.DoesNotContain("\"semanticTokens\":", json);
    }

    [Fact]
    public void SerializeReport_IncludesSemanticTokensWithTheLspRangeAndKind()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var tokens = new List<SemanticToken>
        {
            new(new LspRange(new LspPosition(0, 0), new LspPosition(0, 5)), TokenKind.SpeakerName),
        };

        var json = DisplayGraphJson.SerializeReport(
            "static", null, "Alice: Hi.", [graph], semanticTokens: tokens);

        Assert.Contains(
            "\"range\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":5}}",
            json);
        Assert.Contains("\"kind\":\"SpeakerName\"", json);
    }

    [Fact]
    public void SerializeReport_WritesTheTokenKindAsItsName_NotCamelCased()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var tokens = new List<SemanticToken>
        {
            new(new LspRange(new LspPosition(0, 0), new LspPosition(0, 6)), TokenKind.CustomTag),
        };

        var json = DisplayGraphJson.SerializeReport(
            "static", null, "#happy", [graph], semanticTokens: tokens);

        Assert.Contains("\"kind\":\"CustomTag\"", json);
        Assert.DoesNotContain("\"kind\":\"customTag\"", json);
    }

    [Fact]
    public void SerializeReport_WritesAnEmptySemanticTokensArray_ForADocumentWithNoDialogue()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], semanticTokens: []);

        Assert.Contains("\"semanticTokens\":[]", json);
    }

    [Fact]
    public void SerializeDocument_IncludesSemanticTokens()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var tokens = new List<SemanticToken>
        {
            new(new LspRange(new LspPosition(0, 0), new LspPosition(0, 2)), TokenKind.JumpIndicator),
        };

        var json = DisplayGraphJson.SerializeDocument(
            "view", "scene.dialogue.md", "=>", [graph], semanticTokens: tokens);

        Assert.Contains("\"kind\":\"JumpIndicator\"", json);
    }
}
