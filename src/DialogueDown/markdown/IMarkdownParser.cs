using DialogueDown.Diagnostics;

namespace DialogueDown.Markdown;

/// <summary>
/// Parses a raw script string into a Markdown AST. The rest of the compiler depends
/// only on this interface, so the Markdown library behind it can be swapped
/// without touching downstream code. It takes the compilation's
/// <see cref="DiagnosticsContext"/> like every other stage, so the front end can
/// report what it finds in the source.
/// </summary>
internal interface IMarkdownParser
{
    MarkdownDocument Parse(string source, DiagnosticsContext context);
}
