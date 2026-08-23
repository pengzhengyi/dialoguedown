using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Diagnostics;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Editor;
using DialogueDown.Visualization.Playbook;

namespace DialogueDown.Visualization.Render;

/// <summary>
/// Serializes the report data to the JSON the client script consumes. The default
/// encoder escapes HTML-sensitive characters (<c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c>),
/// so the JSON is safe to inline inside a <c>&lt;script&gt;</c> block — a label or
/// source containing <c>&lt;/script&gt;</c> cannot break out of the tag.
/// </summary>
internal static class DisplayGraphJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(IEnumerable<DisplayGraph> graphs) =>
        JsonSerializer.Serialize(graphs, _options);

    /// <summary>
    /// Serializes the report payload injected into the page — the display
    /// <paramref name="mode"/> (static/watch/live), the document <paramref name="path"/>
    /// when known, the compiled <paramref name="source"/> (shown in the Source tab;
    /// omitted when null), each stage's display graph, the editor's resolved
    /// <paramref name="symbols"/> (omitted when null), the applied
    /// <paramref name="configuration"/> for the Config tab (omitted when null), the
    /// LSP-shaped <paramref name="diagnostics"/> the editor overlay renders (omitted when
    /// null; an empty array clears the overlay after a clean compile), and the
    /// <paramref name="semanticTokens"/> the editor highlights (omitted when null; an empty
    /// array for a document with no dialogue constructs).
    /// </summary>
    public static string SerializeReport(
        string mode,
        string? path,
        string? source,
        IEnumerable<DisplayGraph> stages,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null,
        IReadOnlyList<LspDiagnostic>? diagnostics = null,
        IReadOnlyList<SemanticToken>? semanticTokens = null,
        ConfigStatusOverlay? configOverlay = null,
        ReportProject? project = null,
        PlaybookReport? playbook = null)
    {
        var json = JsonSerializer.Serialize(
            new
            {
                mode,
                path,
                source,
                stages,
                symbols,
                configuration,
                diagnostics,
                semanticTokens,
                project,
                playbook,
            },
            _options);
        return configOverlay is null ? json : ApplyConfigOverlay(json, configOverlay);
    }

    /// <summary>
    /// Serializes the current document payload —
    /// <c>{ mode, path, source, stages, symbols, configuration, diagnostics, semanticTokens }</c>
    /// — for the live server's document API and its hot-reload push events.
    /// </summary>
    public static string SerializeDocument(
        string mode,
        string path,
        string? source,
        IEnumerable<DisplayGraph> stages,
        SymbolSet? symbols = null,
        ConfigurationReport? configuration = null,
        IReadOnlyList<LspDiagnostic>? diagnostics = null,
        IReadOnlyList<SemanticToken>? semanticTokens = null,
        ConfigStatusOverlay? configOverlay = null,
        ReportProject? project = null,
        PlaybookReport? playbook = null)
    {
        var json = JsonSerializer.Serialize(
            new
            {
                mode,
                path,
                source,
                stages,
                symbols,
                configuration,
                diagnostics,
                semanticTokens,
                project,
                playbook,
            },
            _options);
        return configOverlay is null ? json : ApplyConfigOverlay(json, configOverlay);
    }

    // A saved-invalid Config overlay: the graphs and speakers stay the last valid compile, but the
    // Config tab must show the current invalid source and the payload must announce it is stale, so
    // a reload of the page restores the saved-invalid state instead of the last valid text. When the
    // last valid compile had no configuration at all (an invalid file adopted from the no-config
    // state), the configuration section and its file are synthesized from the overlay so the client
    // can still open the Config tab to edit and recover the invalid file.
    private static string ApplyConfigOverlay(string json, ConfigStatusOverlay overlay)
    {
        var node = JsonNode.Parse(json)!.AsObject();
        node["configStatus"] = "saved-invalid";
        node["configMessage"] = overlay.Message;

        if (node["configuration"] is not JsonObject configuration)
        {
            configuration = new JsonObject { ["speakers"] = new JsonArray() };
            node["configuration"] = configuration;
        }

        if (configuration["file"] is JsonObject file)
        {
            file["source"] = overlay.Source;
        }
        else
        {
            configuration["file"] = new JsonObject
            {
                ["path"] = overlay.Path,
                ["source"] = overlay.Source,
            };
        }

        return node.ToJsonString();
    }
}
