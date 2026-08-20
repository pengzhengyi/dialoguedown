using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DialogueDown.Visualization.Render;

/// <summary>
/// The constant half of the report page — the client script and stylesheet the <c>web/</c> Vite
/// project builds — lifted out of the page so a served report can link them instead of inlining
/// them. Only the per-document payload then varies between pages, so a browser downloads and
/// compiles the client once and reuses it for every script it opens. An exported report still
/// inlines everything: a file that has to work offline cannot link anything.
/// </summary>
/// <remarks>
/// Each asset is named after a hash of its own content, so a rebuilt client is a different URL.
/// That is what lets the server promise the assets never change, without ever serving a stale one.
/// </remarks>
internal sealed partial class ReportBundle
{
    private const string AssetRoot = "/assets/";

    private ReportBundle(string script, string style, string scriptPath, string stylePath, string linkedHtml)
    {
        Script = script;
        Style = style;
        ScriptPath = scriptPath;
        StylePath = stylePath;
        LinkedHtml = linkedHtml;
    }

    /// <summary>The split of the report page embedded in this assembly.</summary>
    public static ReportBundle Default { get; } = From(EmbeddedAsset.ReadText("report.html"));

    /// <summary>The built client script, without its surrounding tag.</summary>
    public string Script { get; }

    /// <summary>The built stylesheet, without its surrounding tag.</summary>
    public string Style { get; }

    /// <summary>The content-addressed path the page requests <see cref="Script"/> from.</summary>
    public string ScriptPath { get; }

    /// <summary>The content-addressed path the page requests <see cref="Style"/> from.</summary>
    public string StylePath { get; }

    /// <summary>The page with both assets replaced by references to them.</summary>
    public string LinkedHtml { get; }

    /// <summary>Splits <paramref name="reportHtml"/> into its constant assets and the page that links them.</summary>
    public static ReportBundle From(string reportHtml)
    {
        ArgumentNullException.ThrowIfNull(reportHtml);
        var script = BuiltScript().Match(reportHtml);
        var style = BuiltStyle().Match(reportHtml);
        if (!script.Success || !style.Success)
        {
            throw new InvalidOperationException(
                "The built report no longer holds one module script and one stylesheet to lift out.");
        }

        var scriptBody = script.Groups["body"].Value;
        var styleBody = style.Groups["body"].Value;
        var scriptPath = AssetPath(scriptBody, "js");
        var stylePath = AssetPath(styleBody, "css");

        // Keep `type="module"`: the payload is assigned by an inline script at the end of the
        // body, and only a module still runs after the document is parsed.
        var linked = reportHtml
            .Replace(script.Value, $"<script type=\"module\" crossorigin src=\"{scriptPath}\"></script>", StringComparison.Ordinal)
            .Replace(style.Value, $"<link rel=\"stylesheet\" crossorigin href=\"{stylePath}\" />", StringComparison.Ordinal);

        return new ReportBundle(scriptBody, styleBody, scriptPath, stylePath, linked);
    }

    private static string AssetPath(string content, string extension)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var name = Convert.ToHexString(digest, 0, 8).ToLowerInvariant();
        return string.Create(
            CultureInfo.InvariantCulture, $"{AssetRoot}report.{name}.{extension}");
    }

    [GeneratedRegex("""<script type="module"[^>]*>(?<body>.*?)</script>""", RegexOptions.Singleline)]
    private static partial Regex BuiltScript();

    [GeneratedRegex("""<style rel="stylesheet"[^>]*>(?<body>.*?)</style>""", RegexOptions.Singleline)]
    private static partial Regex BuiltStyle();
}
