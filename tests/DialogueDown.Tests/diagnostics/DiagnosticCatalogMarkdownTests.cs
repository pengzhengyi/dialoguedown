using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCatalogMarkdownTests
{
    private static readonly string _rendered = DiagnosticCatalogMarkdown.Render();

    // The page's headings, in order. Example scripts carry their own "# Scene" lines inside <pre>
    // blocks; those are script text, not headings, so they are left out.
    private static readonly IReadOnlyList<string> _headings =
        [.. WithoutPreBlocks(_rendered).Split('\n').Where(line => line.StartsWith('#'))];

    [Fact]
    public void OpensWithASingleErrorCodesTitle()
    {
        Assert.StartsWith("# Error codes\n", _rendered);
        Assert.Single(_headings, heading => heading.StartsWith("# ", StringComparison.Ordinal));
    }

    [Fact]
    public void RendersEveryCatalogCodeAsAnAnchoredSubsection()
    {
        foreach (var descriptor in DiagnosticCatalogReflection.Descriptors())
        {
            // A bare code heading gives a stable slug (### DLG2001 -> #dlg2001) for deep links.
            Assert.Contains($"### {descriptor.Code}\n", _rendered);
        }
    }

    [Fact]
    public void ShowsEachDescriptorsTitleSeverityAndMessage()
    {
        foreach (var descriptor in DiagnosticCatalogReflection.Descriptors())
        {
            Assert.Contains($">{descriptor.DefaultSeverity}</span> · {descriptor.Title}", _rendered);
            Assert.Contains(descriptor.MessageFormat, _rendered);
        }
    }

    [Fact]
    public void MarksTheChangedTokensInADocumentedExample()
    {
        // DLG1101 marks the offending tags in red in the broken script and the added speaker name
        // in green in the fix, so the change stands out in both examples.
        Assert.Contains("<mark class=\"dd-mark-bad\">#excited</mark>", _rendered);
        Assert.Contains("<mark class=\"dd-mark-fix\">Alice </mark>#excited", _rendered);
        Assert.Contains("<span class=\"dd-eg-fix\">Fix</span>", _rendered);
        Assert.Contains("<span class=\"dd-eg-bad\">Triggering example</span>", _rendered);
    }

    [Fact]
    public void GroupsCodesUnderTheirCategoryHeadingInReportingOrder()
    {
        var sectionOf = new Dictionary<DiagnosticCategory, string>
        {
            [DiagnosticCategory.Syntax] = "## Syntax (`DLG1xxx`)",
            [DiagnosticCategory.Semantic] = "## Semantic (`DLG2xxx`)",
            [DiagnosticCategory.Style] = "## Style (`DLG3xxx`)",
        };
        var sections = CodesBySection();

        Assert.Equal(
            ["## Syntax (`DLG1xxx`)", "## Semantic (`DLG2xxx`)", "## Style (`DLG3xxx`)"],
            sections.Select(section => section.Heading));
        var codesUnder = sections.ToDictionary(section => section.Heading, section => section.Codes);
        Assert.All(
            Enum.GetValues<DiagnosticCategory>(),
            category => Assert.Equal(
                DiagnosticCatalogReflection.Descriptors()
                    .Where(descriptor => descriptor.Category == category)
                    .Select(descriptor => descriptor.Code)
                    .Order(StringComparer.Ordinal),
                codesUnder[sectionOf[category]].Order(StringComparer.Ordinal)));
    }

    [Fact]
    public void OrdersCodesWithinACategoryAscending()
    {
        Assert.All(
            CodesBySection(),
            section => Assert.Equal(section.Codes.Order(StringComparer.Ordinal), section.Codes));
    }

    [Fact]
    public void IsDeterministic()
    {
        Assert.Equal(_rendered, DiagnosticCatalogMarkdown.Render());
    }

    // Each "##" heading, in page order, with the "###" codes beneath it, in page order. A code
    // above the first section belongs to none and is left out.
    private static List<(string Heading, List<string> Codes)> CodesBySection()
    {
        List<(string Heading, List<string> Codes)> sections = [];
        List<string> codes = [];
        foreach (var heading in _headings)
        {
            if (heading.StartsWith("## ", StringComparison.Ordinal))
            {
                codes = [];
                sections.Add((heading, codes));
            }
            else if (heading.StartsWith("### ", StringComparison.Ordinal))
            {
                codes.Add(heading["### ".Length..]);
            }
        }

        return sections;
    }

    private static string WithoutPreBlocks(string markdown) =>
        System.Text.RegularExpressions.Regex.Replace(
            markdown, "<pre[^>]*>.*?</pre>", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
}
