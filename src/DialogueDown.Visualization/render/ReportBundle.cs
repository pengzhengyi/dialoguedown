using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DialogueDown.Visualization.Render;

/// <summary>
/// The constant halves of a report: the client script, its stylesheet, and Mermaid's own build.
/// A served report links them, so a browser downloads and compiles the client once and reuses it
/// for every script it opens; an exported report inlines them, because a file that leaves the
/// server has to work offline. Mermaid stays apart from the client in both shapes — it is the
/// largest thing a report can draw with and the rarest thing it needs.
/// </summary>
/// <remarks>
/// Each asset is named after a hash of its own content, so a rebuilt client is a different URL.
/// That is what lets the server promise the assets never change, without ever serving a stale one.
/// </remarks>
internal sealed class ReportBundle
{
    private const string AssetRoot = "/assets/";

    private ReportBundle(string page, string script, string style, string mermaid)
    {
        Page = page;
        Script = script;
        Style = style;
        Mermaid = mermaid;
        ScriptPath = PathFor(script, "js");
        StylePath = PathFor(style, "css");
        MermaidPath = PathFor(mermaid, "js");
    }

    /// <summary>The report as built, with its assets kept apart from the page.</summary>
    public static ReportBundle Default { get; } = new(
        EmbeddedAsset.ReadText("report.html"),
        EmbeddedAsset.ReadText("report.js"),
        EmbeddedAsset.ReadText("report.css"),
        EmbeddedAsset.ReadText("mermaid.js"));

    /// <summary>The built page, before its assets are either linked or inlined.</summary>
    public string Page { get; }

    /// <summary>The built client script.</summary>
    public string Script { get; }

    /// <summary>The built stylesheet, with fonts and icons already inlined.</summary>
    public string Style { get; }

    /// <summary>Mermaid's self-contained build, which assigns <c>globalThis.mermaid</c>.</summary>
    public string Mermaid { get; }

    /// <summary>The content-addressed path a served page requests <see cref="Script"/> from.</summary>
    public string ScriptPath { get; }

    /// <summary>The content-addressed path a served page requests <see cref="Style"/> from.</summary>
    public string StylePath { get; }

    /// <summary>The content-addressed path a served page requests <see cref="Mermaid"/> from.</summary>
    public string MermaidPath { get; }

    /// <summary>The path <paramref name="content"/> is addressed by, given its file extension.</summary>
    public static string PathFor(string content, string extension)
    {
        ArgumentNullException.ThrowIfNull(content);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var name = Convert.ToHexString(digest, 0, 8).ToLowerInvariant();
        return string.Create(CultureInfo.InvariantCulture, $"{AssetRoot}report.{name}.{extension}");
    }
}
