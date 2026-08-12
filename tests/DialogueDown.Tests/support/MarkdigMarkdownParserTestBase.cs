using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Base for parser test classes: provides one shared parser instance and threads a fresh
/// diagnostics context per call, so each feature-focused test class does not repeat the setup.
/// </summary>
public abstract class MarkdigMarkdownParserTestBase
{
    private protected IMarkdownParser Parser { get; } = MarkdownParserFactory.MarkdownParser();

    private protected MarkdownDocument Parse(string source) =>
        Parser.Parse(source, DiagnosticsContextFactory.Context(source));
}
