using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds the front-end Markdown parser for tests in one place, so its construction is not
/// repeated across test classes, mirroring <see cref="TranspilerBuilderFactory"/>.
/// </summary>
internal static class MarkdownParserFactory
{
    public static IMarkdownParser MarkdownParser() =>
        new MarkdigMarkdownParser(DefaultUnmodeledNodeHandlingPolicy.Instance);

    /// <summary>Parses with the default policy and a throwaway diagnostics context.</summary>
    public static MarkdownDocument Parse(string source) =>
        MarkdownParser().Parse(source, DiagnosticsContextFactory.Context(source));

    /// <summary>Parses under a policy that deviates from the default.</summary>
    public static MarkdownDocument Parse(string source, IUnmodeledNodeHandlingPolicy policy) =>
        new MarkdigMarkdownParser(policy).Parse(source, DiagnosticsContextFactory.Context(source));
}
