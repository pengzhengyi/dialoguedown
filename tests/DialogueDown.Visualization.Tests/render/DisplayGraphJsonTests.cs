using System.Text.Json.Nodes;
using DialogueDown.Visualization.Diagnostics;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Lsp;
using DialogueDown.Visualization.Render;
using static DialogueDown.TestSupport.JsonAssert;
using static DialogueDown.TestSupport.ReportPayload;
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

        AssertJson(
            """
            [{
              "title": "Markdown AST",
              "description": "",
              "nodes": [
                { "id": "n0", "label": "Document", "attributes": [] },
                { "id": "n1", "label": "Heading", "attributes": [{ "name": "level", "value": "2" }] }
              ],
              "edges": [{ "fromId": "n0", "toId": "n1", "kind": "Child" }],
              "regions": [],
              "nests": true,
              "readsDialogueMeaning": false
            }]
            """,
            JsonNode.Parse(json));
    }

    [Fact]
    public void Serialize_CarriesWhetherTheStageNests()
    {
        // The client reads this to decide how a reverse jump finds the node enclosing a selection.
        var nesting = MakeGraph("Markdown AST", [Node("n0", "Document")], []);
        var flow = MakeGraph("Dialogue Graph", [Node("n0", "Line")], []) with { Nests = false };

        Assert.True((bool?)OnlyStage(DisplayGraphJson.Serialize([nesting]))["nests"]);
        Assert.False((bool?)OnlyStage(DisplayGraphJson.Serialize([flow]))["nests"]);
    }

    [Fact]
    public void Serialize_CarriesWhetherTheStageHasReadDialogueMeaning()
    {
        // The client reads this to decide whether `=>` renders as the jump ligature or plain text.
        var plain = MakeGraph("Markdown AST", [Node("n0", "Document")], []);
        var dialogue =
            MakeGraph("Dialogue AST", [Node("n0", "Line")], []) with { ReadsDialogueMeaning = true };

        Assert.False((bool?)OnlyStage(DisplayGraphJson.Serialize([plain]))["readsDialogueMeaning"]);
        Assert.True((bool?)OnlyStage(DisplayGraphJson.Serialize([dialogue]))["readsDialogueMeaning"]);
    }

    [Fact]
    public void Serialize_IncludesStageDescription()
    {
        var graph = MakeGraph(
            "Markdown AST", [Node("n0", "Document")], [], description: "What it shows.");

        var stage = OnlyStage(DisplayGraphJson.Serialize([graph]));

        Assert.Equal("What it shows.", (string?)stage["description"]);
    }

    [Fact]
    public void Serialize_IncludesUnavailableReasonForADisabledStage()
    {
        var graph = DisplayGraph.ForUnavailableStage(
            "Semantic Model", "What it would show.", "Unavailable due to compilation errors.");

        var stage = OnlyStage(DisplayGraphJson.Serialize([graph]));

        AssertJson("""{ "reason": "Unavailable due to compilation errors." }""", stage["unavailable"]);
        AssertJson("[]", stage["nodes"]);
    }

    [Fact]
    public void Serialize_OmitsUnavailableForAProducedStage()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        Assert.DoesNotContain("unavailable", json); // not on the stage, and nowhere beneath it
    }

    [Fact]
    public void SerializeReport_WithProject_IncludesRootAndActivePath()
    {
        var json = DisplayGraphJson.SerializeReport(
            "view", "act-1/prologue.dialogue.md", "Alice: hi", [],
            project: new ReportProject("/project/root", "act-1/prologue.dialogue.md"));

        AssertJson(
            """{ "root": "/project/root", "activePath": "act-1/prologue.dialogue.md" }""",
            Parse(json).Json["project"]);
    }

    [Fact]
    public void SerializeReport_WithoutProject_OmitsTheProjectContext()
    {
        var json = DisplayGraphJson.SerializeReport("view", "a.dialogue.md", "Alice: hi", []);

        AssertOmits(Parse(json).Json, "project");
    }

    [Fact]
    public void Serialize_IncludesNodeSourceWhenPresentAndOmitsWhenNull()
    {
        var graph = MakeGraph(
            "G",
            [new DisplayNode("n0", "Text", [], "# Hi"), Node("n1", "Empty")],
            []);

        var nodes = OnlyStage(DisplayGraphJson.Serialize([graph]))["nodes"]!;

        Assert.Equal("# Hi", (string?)nodes[0]!["source"]);
        AssertOmits(nodes[1], "source");
    }

    [Fact]
    public void Serialize_IncludesNodeCategoryWhenPresent()
    {
        var graph = MakeGraph("G", [new DisplayNode("n0", "Code span", [], null, "call")], []);

        var node = OnlyStage(DisplayGraphJson.Serialize([graph]))["nodes"]![0]!;

        Assert.Equal("call", (string?)node["category"]);
    }

    [Fact]
    public void Serialize_IncludesNodeEntityKeyWhenPresentAndOmitsWhenNull()
    {
        var graph = MakeGraph(
            "G",
            [new DisplayNode("n0", "The Market", [], null, "structure", "scene:the-market"), Node("n1", "Plain")],
            []);

        var nodes = OnlyStage(DisplayGraphJson.Serialize([graph]))["nodes"]!;

        Assert.Equal("scene:the-market", (string?)nodes[0]!["entityKey"]);
        AssertOmits(nodes[1], "entityKey");
    }

    [Fact]
    public void Serialize_OmitsTablesWhenNullAndIncludesThemWhenPresent()
    {
        var plain = MakeGraph("G", [Node("n0", "Document")], []);
        AssertOmits(OnlyStage(DisplayGraphJson.Serialize([plain])), "tables");

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
        var stage = OnlyStage(DisplayGraphJson.Serialize([withTables]));

        AssertJson(
            """
            [{
              "title": "Anchors",
              "columns": ["Anchor", "Scene"],
              "rows": [{
                "cells": [{ "text": "#the-market", "copyable": false }, { "text": "The Market", "copyable": false }],
                "entityKey": "scene:the-market"
              }],
              "emptyText": "No scenes.",
              "facetColumns": []
            }]
            """,
            stage["tables"]);
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
                    [new SemanticRow([new SemanticCell("→ The Market", RefKey: "scene:the-market")])],
                    "No jumps."),
            ],
        };

        var table = OnlyStage(DisplayGraphJson.Serialize([graph]))["tables"]![0]!;
        var cell = table["rows"]![0]!["cells"]![0]!;

        Assert.Equal("scene:the-market", (string?)cell["refKey"]);
    }

    [Fact]
    public void Serialize_EscapesHtmlSensitiveCharacters_SoScriptCannotBreakOut()
    {
        var graph = MakeGraph("G", [Node("n0", "</script><b>&")], []);

        var json = DisplayGraphJson.Serialize([graph]);

        // This one is about the text itself: the page inlines it inside a <script> element.
        Assert.DoesNotContain("</script>", json);
        Assert.Contains("\\u003C", json); // '<' is unicode-escaped
        Assert.Equal("</script><b>&", (string?)OnlyStage(json)["nodes"]![0]!["label"]); // and reads back unchanged
    }

    [Fact]
    public void SerializeReport_WrapsModeSourceAndStages()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport("static", null, "# Hello", [graph]));

        Assert.Equal("static", report.Mode);
        Assert.Equal("# Hello", report.Source);
        Assert.Equal("Markdown AST", (string?)Assert.Single(report.Stages!)!["title"]);
    }

    [Fact]
    public void SerializeReport_OmitsSourceWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport("static", null, null, [graph]));

        AssertOmits(report.Json, "source");
        Assert.NotNull(report.Stages);
    }

    [Fact]
    public void SerializeReport_WithModeAndPath_AddsBoth()
    {
        var graph = MakeGraph("Markdown AST", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport("view", "scene.dialogue.md", "# Hi", [graph]));

        Assert.Equal("view", report.Mode);
        Assert.Equal("scene.dialogue.md", report.Path);
        Assert.Equal("# Hi", report.Source);
    }

    [Fact]
    public void SerializeReport_WithoutPath_OmitsPath()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]));

        AssertOmits(report.Json, "path");
    }

    [Fact]
    public void SerializeReport_OmitsSymbolsWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]));

        AssertOmits(report.Json, "symbols");
    }

    [Fact]
    public void SerializeReport_IncludesSymbolsWhenPresent()
    {
        var graph = MakeGraph("Semantic Model", [Node("n0", "Document")], []);
        var symbols = new SymbolSet(
            [new JumpTargetSymbol("the-market", "The Market")],
            ["Guide"],
            ["guide"],
            ["wise"],
            [new ReservedTargetSymbol("END", "End", ReservedTargetRole.Terminal)]);

        var report = Parse(DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph], symbols));

        AssertJson(
            """
            {
              "jumpTargets": [{ "slug": "the-market", "heading": "The Market" }],
              "speakers": ["Guide"],
              "speakerIds": ["guide"],
              "tags": ["wise"],
              "reservedTargets": [{ "anchor": "END", "label": "End", "role": "Terminal" }]
            }
            """,
            report.Symbols);
    }

    [Fact]
    public void SerializeReport_OmitsDiagnosticsWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]));

        AssertOmits(report.Json, "diagnostics");
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

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: diagnostics));

        AssertJson(
            """
            [{
              "range": { "start": { "line": 2, "character": 0 }, "end": { "line": 2, "character": 8 } },
              "severity": 1,
              "code": "DLG2001",
              "message": "Two scenes resolve to the same anchor '#chapter'.",
              "source": "dialoguedown"
            }]
            """,
            report.Diagnostics);
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

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: diagnostics));

        AssertJson("1", report.Diagnostics![0]!["severity"]); // the number, not "Error"
    }

    [Fact]
    public void SerializeReport_IncludesTheFixTitleAndRelativeEdits()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var diagnostics = new List<LspDiagnostic>
        {
            new(
                new LspRange(new LspPosition(2, 0), new LspPosition(2, 2)),
                LspSeverity.Warning,
                "DLG1113",
                "Dangling arrow.",
                "dialoguedown",
                Fixes: [new LspFix("Escape as literal text", [new LspEdit(0, 0, "\\")])]),
        };

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: diagnostics));

        AssertJson(
            """[{ "title": "Escape as literal text", "edits": [{ "start": 0, "end": 0, "newText": "\\" }] }]""",
            report.Diagnostics![0]!["fixes"]);
    }

    [Fact]
    public void SerializeReport_OmitsFixesWhenADiagnosticHasNone()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var diagnostics = new List<LspDiagnostic>
        {
            new(
                new LspRange(new LspPosition(0, 0), new LspPosition(0, 1)),
                LspSeverity.Error, "DLG0001", "Boom.", "dialoguedown"),
        };

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: diagnostics));

        AssertOmits(report.Diagnostics![0], "fixes");
    }

    [Fact]
    public void SerializeReport_WritesAnEmptyDiagnosticsArray_SoACleanCompileClearsTheOverlay()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], diagnostics: []));

        AssertJson("[]", report.Diagnostics);
    }

    [Fact]
    public void SerializeReport_OmitsSemanticTokensWhenNull()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport("static", null, "# Hi", [graph]));

        AssertOmits(report.Json, "semanticTokens");
    }

    [Fact]
    public void SerializeReport_IncludesSemanticTokensWithTheLspRangeAndKind()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var tokens = new List<SemanticToken>
        {
            new(new LspRange(new LspPosition(0, 0), new LspPosition(0, 5)), TokenKind.SpeakerName),
        };

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "Alice: Hi.", [graph], semanticTokens: tokens));

        AssertJson(
            """
            [{
              "range": { "start": { "line": 0, "character": 0 }, "end": { "line": 0, "character": 5 } },
              "kind": "SpeakerName"
            }]
            """,
            report.SemanticTokens);
    }

    [Fact]
    public void SerializeReport_WritesTheTokenKindAsItsName_NotCamelCased()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);
        var tokens = new List<SemanticToken>
        {
            new(new LspRange(new LspPosition(0, 0), new LspPosition(0, 6)), TokenKind.CustomTag),
        };

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "#happy", [graph], semanticTokens: tokens));

        Assert.Equal("CustomTag", (string?)report.SemanticTokens![0]!["kind"]);
    }

    [Fact]
    public void SerializeReport_WritesAnEmptySemanticTokensArray_ForADocumentWithNoDialogue()
    {
        var graph = MakeGraph("G", [Node("n0", "Document")], []);

        var report = Parse(DisplayGraphJson.SerializeReport(
            "static", null, "# Hi", [graph], semanticTokens: []));

        AssertJson("[]", report.SemanticTokens);
    }

    // The one stage a serialized list of stages holds.
    private static JsonObject OnlyStage(string json) =>
        Assert.IsType<JsonObject>(Assert.Single(Assert.IsType<JsonArray>(JsonNode.Parse(json))));
}
