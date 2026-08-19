using DialogueDown.ConfigurationLoader.Errors;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Toml;

/// <summary>
/// Parses TOML text into Tomlyn's round-trippable syntax tree, bound to a source name so
/// diagnostics can point back at a file. It is the single seam that touches Tomlyn's parser, and
/// it turns a TOML syntax error into a located <see cref="DialogueConfigurationException"/>.
/// </summary>
internal sealed class TomlDocumentParser
{
    private readonly string _sourceName;

    public TomlDocumentParser(string sourceName) => _sourceName = sourceName;

    public DocumentSyntax Parse(string toml)
    {
        var document = SyntaxParser.Parse(toml, _sourceName);
        if (document.HasErrors)
        {
            ThrowSyntaxError(document);
        }

        return document;
    }

    private static void ThrowSyntaxError(DocumentSyntax document)
    {
        // Fail fast on the first error, like every other compiler stage. Filter to Error kind so a
        // leading warning is never mistaken for the failure; collecting every diagnostic waits for
        // the planned repo-wide diagnostics phase.
        var error = document.Diagnostics.First(
            diagnostic => diagnostic.Kind == DiagnosticMessageKind.Error);
        throw new DialogueConfigurationException(error.Message, TomlLocation.From(error.Span));
    }
}
