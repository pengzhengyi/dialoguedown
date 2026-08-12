using DialogueDown.Diagnostics;
using Markdig;
using Markdig.Extensions.EmphasisExtras;

namespace DialogueDown.Markdown;

/// <summary>
/// Parses a script with the Markdig library and converts its tree into our own
/// Markdown AST. The pipeline is CommonMark plus pipe tables (so a table can be
/// recognized and then handled per policy); emphasis is parsed so styling can be
/// modeled. An <see cref="IUnmodeledNodeHandlingPolicy"/> decides whether each
/// unmodeled construct is kept or ignored, and every ignored construct is noted
/// in the compilation's diagnostics.
/// </summary>
internal sealed class MarkdigMarkdownParser : IMarkdownParser
{
    private static readonly MarkdownPipeline _pipeline = BuildPipeline();

    private readonly IUnmodeledNodeHandlingPolicy _policy;

    public MarkdigMarkdownParser(IUnmodeledNodeHandlingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _policy = policy;
    }

    public MarkdownDocument Parse(string source, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);

        var parsed = Markdig.Markdown.Parse(source, _pipeline);
        var unmodeled = new MarkdigUnmodeledNodeHandler(source, _policy, context.Diagnostics);
        return new MarkdigToMarkdownAstConverter(unmodeled).Convert(parsed);
    }

    private static MarkdownPipeline BuildPipeline() =>
        new MarkdownPipelineBuilder()
            .UsePreciseSourceLocation()
            .UsePipeTables()
            .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
            .UseYamlFrontMatter()
            .Build();
}
