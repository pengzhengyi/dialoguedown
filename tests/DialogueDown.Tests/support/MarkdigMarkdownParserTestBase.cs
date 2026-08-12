using DialogueDown.Diagnostics;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Base for parser test classes: provides one shared parser instance and threads a fresh
/// diagnostics context per call, so each feature-focused test class does not repeat the setup.
/// The <c>out</c> overload hands back the bag, so a test can assert what the front end reported.
/// </summary>
public abstract class MarkdigMarkdownParserTestBase
{
    private protected IMarkdownParser Parser { get; } = MarkdownParserFactory.MarkdownParser();

    private protected MarkdownDocument Parse(string source) =>
        Parser.Parse(source, DiagnosticsContextFactory.Context(source));

    private protected MarkdownDocument Parse(string source, out DiagnosticBag diagnostics) =>
        Parser.Parse(source, DiagnosticsContextFactory.Context(out diagnostics, source));
}
