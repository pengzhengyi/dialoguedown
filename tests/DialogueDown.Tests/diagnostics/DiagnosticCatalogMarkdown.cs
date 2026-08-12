using System.Net;
using System.Text;
using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

/// <summary>
/// Renders the diagnostic catalog as the user-facing "Error codes" reference page. The page is a
/// generated artifact: <see cref="DiagnosticCatalogDocumentationTests"/> asserts the committed
/// <c>docs/guide/error-codes.md</c> matches this output, so the reference never drifts from the
/// catalog. Grouped by category and sorted by code, with one anchored subsection per code so a
/// tool can deep-link a diagnostic (for example, <c>#dlg2001</c>).
/// </summary>
internal static class DiagnosticCatalogMarkdown
{
    private const string Intro =
        "# Error codes\n"
        + "\n"
        + "DialogueDown reports each problem it finds as a **diagnostic** with a stable `DLG####` "
        + "code, so a message is easy to look up. A code's leading digit names its category — "
        + "`DLG1xxx` syntax, `DLG2xxx` semantic, `DLG3xxx` style — and each diagnostic has a "
        + "default severity: "
        + "<span class=\"dd-sev dd-sev--error\">Error</span> (must be fixed), "
        + "<span class=\"dd-sev dd-sev--warning\">Warning</span> (compiles but is suspect), or "
        + "<span class=\"dd-sev dd-sev--info\">Info</span> (a neutral note). Placeholders such as "
        + "`{0}` are filled with specifics — a name, a count — when the message is shown.\n";

    // Categories in reporting order, each with the range its codes occupy and a one-line summary.
    // A category with no descriptors yet (Style) is listed here but only rendered once it has one.
    private static readonly IReadOnlyList<CategorySection> _sections =
    [
        new(
            DiagnosticCategory.Syntax,
            "DLG1xxx",
            "The script's surface: text that does not parse as intended, or Markdown that never "
            + "becomes dialogue."),
        new(
            DiagnosticCategory.Semantic,
            "DLG2xxx",
            "A meaning-level problem found during analysis — a reference that does not resolve, "
            + "or a conflict."),
        new(DiagnosticCategory.Style, "DLG3xxx", "A valid script that reads correctly but could read better."),
    ];

    public static string Render()
    {
        var byCategory = DiagnosticCatalogReflection.Descriptors()
            .GroupBy(descriptor => descriptor.Category)
            .ToDictionary(group => group.Key, group => group.OrderBy(d => d.Code, StringComparer.Ordinal).ToList());

        var builder = new StringBuilder(Intro);
        foreach (var section in _sections)
        {
            if (!byCategory.TryGetValue(section.Category, out var descriptors))
            {
                continue;
            }

            AppendSection(builder, section, descriptors);
        }

        return builder.ToString();
    }

    private static void AppendSection(
        StringBuilder builder, CategorySection section, IReadOnlyList<DiagnosticDescriptor> descriptors)
    {
        builder.Append($"\n## {section.Category} (`{section.Range}`)\n\n{section.Summary}\n");
        foreach (var descriptor in descriptors)
        {
            AppendDiagnostic(builder, descriptor);
        }
    }

    private static void AppendDiagnostic(StringBuilder builder, DiagnosticDescriptor descriptor)
    {
        builder.Append(
            $"\n### {descriptor.Code}\n\n{SeverityBadge(descriptor.DefaultSeverity)} · "
            + $"{descriptor.Title}\n\n{descriptor.MessageFormat}\n");

        if (!DiagnosticDocs.ByCode.TryGetValue(descriptor.Code, out var doc))
        {
            return;
        }

        builder.Append($"\n{doc.Explanation}\n");
        if (doc.Example is { } example)
        {
            AppendExample(
                builder, "dd-eg-bad", "Triggering example", "dd-mark-bad",
                example.Broken, example.BrokenHighlights);
            AppendExample(
                builder, "dd-eg-fix", FixLabel(example.FixWhen), "dd-mark-fix",
                example.Fixed, example.FixedHighlights);
            if (example.Alternative is { } alternative)
            {
                AppendExample(
                    builder, "dd-eg-fix", FixLabel(alternative.When), "dd-mark-fix",
                    alternative.Fixed, alternative.Highlights);
            }
        }
    }

    // A diagnostic with one remedy just says "Fix"; one whose right edit depends on what the writer
    // meant labels each with the case it applies to.
    private static string FixLabel(string? when) => when is null ? "Fix" : $"Fix — {when}";

    // Renders one example as an HTML code block so the changed tokens can be <mark>-highlighted; a
    // fenced block cannot carry inline markup. The label and the marks are themed (red for the broken
    // script, green for the fix) so the meaning survives in both light and dark themes.
    private static void AppendExample(
        StringBuilder builder,
        string labelClass,
        string label,
        string markClass,
        string example,
        IReadOnlyList<string> highlights)
    {
        builder.Append($"\n<span class=\"{labelClass}\">{label}</span>\n\n");
        builder.Append(
            $"<pre class=\"dd-example\"><code class=\"nohighlight\">"
            + $"{HighlightedHtml(example, highlights, markClass)}</code></pre>\n");
    }

    // HTML-escapes the example, then wraps the first occurrence of each highlight in a themed <mark>.
    // Escaping the highlight too keeps the match correct when the source contains HTML-significant
    // characters.
    private static string HighlightedHtml(
        string example, IReadOnlyList<string> highlights, string markClass)
    {
        var html = WebUtility.HtmlEncode(example);
        foreach (var highlight in highlights)
        {
            var encoded = WebUtility.HtmlEncode(highlight);
            var index = html.IndexOf(encoded, StringComparison.Ordinal);
            if (index >= 0)
            {
                html = string.Concat(
                    html[..index], $"<mark class=\"{markClass}\">", encoded, "</mark>",
                    html[(index + encoded.Length)..]);
            }
        }

        return html;
    }

    private static string SeverityBadge(DiagnosticSeverity severity)
    {
        var modifier = severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info",
        };

        return $"<span class=\"dd-sev dd-sev--{modifier}\">{severity}</span>";
    }

    private sealed record CategorySection(DiagnosticCategory Category, string Range, string Summary);
}
