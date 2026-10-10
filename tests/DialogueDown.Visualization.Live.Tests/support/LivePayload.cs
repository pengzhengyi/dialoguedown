using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>
/// A JSON payload a live session sends, read by the fields a test asks about rather than searched
/// as text.
/// </summary>
/// <remarks>
/// A report document carries <c>mode</c>, <c>path</c>, <c>source</c>, <c>stages</c>, and
/// <c>configuration</c>; a save or reload result adds <c>outcome</c>; a saved but invalid
/// configuration adds <c>configStatus</c> and <c>configMessage</c>; a problem carries
/// <c>message</c> and <c>target</c>.
/// </remarks>
internal sealed class LivePayload
{
    private const string PageSlot = "window.__DD_REPORT__ = ";

    private LivePayload(JsonObject json) => Json = json;

    /// <summary>Gets the whole payload, for a test that compares two of them.</summary>
    public JsonObject Json { get; }

    /// <summary>Gets what a save, reload, or config creation came to, such as <c>saved</c>.</summary>
    public string? Outcome => Text("outcome");

    /// <summary>Gets whether the report opened to view or to edit.</summary>
    public string? Mode => Text("mode");

    /// <summary>Gets the path the report shows for its document.</summary>
    public string? Path => Text("path");

    /// <summary>Gets the document's source.</summary>
    public string? Source => Text("source");

    /// <summary>Gets the compiler stages the report shows, or <c>null</c> when it carries none.</summary>
    public JsonArray? Stages => Json["stages"] as JsonArray;

    /// <summary>Gets <c>saved-invalid</c> when the configuration on disk does not parse, or <c>null</c>.</summary>
    public string? ConfigStatus => Text("configStatus");

    /// <summary>Gets why the configuration on disk does not parse, or <c>null</c>.</summary>
    public string? ConfigMessage => Text("configMessage");

    /// <summary>Gets the path of the configuration file the report shows, or <c>null</c>.</summary>
    public string? ConfigPath => Json["configuration"]?["file"]?["path"]?.GetValue<string>();

    /// <summary>Gets the text of the configuration file the report shows, or <c>null</c>.</summary>
    public string? ConfigSource => Json["configuration"]?["file"]?["source"]?.GetValue<string>();

    /// <summary>Gets the names of the speakers the applied configuration declares.</summary>
    public IReadOnlyList<string> SpeakerNames =>
        Json["configuration"]?["speakers"] is JsonArray speakers
            ? [.. speakers.Select(speaker => speaker!["name"]!.GetValue<string>())]
            : [];

    /// <summary>Gets what a problem says went wrong.</summary>
    public string? Message => Text("message");

    /// <summary>Gets which editor a problem is for: <c>document</c> or <c>config</c>.</summary>
    public string? Target => Text("target");

    /// <summary>Reads a payload a session returned or broadcast.</summary>
    /// <param name="json">The payload's JSON text.</param>
    /// <returns>The payload.</returns>
    public static LivePayload Parse(string json) => new(Assert.IsType<JsonObject>(JsonNode.Parse(json)));

    /// <summary>Reads the report a page carries in its data slot.</summary>
    /// <param name="html">The page.</param>
    /// <returns>The report the page embeds.</returns>
    public static LivePayload FromPage(string html)
    {
        var slot = html.IndexOf(PageSlot, StringComparison.Ordinal);
        Assert.True(slot >= 0, "The page carries no report in its data slot.");
        Assert.True(
            html.IndexOf(PageSlot, slot + PageSlot.Length, StringComparison.Ordinal) < 0,
            "The page fills its data slot more than once.");

        // Read exactly one JSON value where the slot starts, and stop at its end.
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(html[(slot + PageSlot.Length)..]));
        using var report = JsonDocument.ParseValue(ref reader);

        return Parse(report.RootElement.GetRawText());
    }

    private string? Text(string field) => Json[field]?.GetValue<string>();
}
