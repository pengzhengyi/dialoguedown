using DialogueDown.ConfigurationLoader.Errors;
using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Toml;

/// <summary>
/// Creates configuration errors located at the TOML syntax node that violated the schema. Readers
/// supply the domain-specific message.
/// </summary>
internal static class TomlErrors
{
    public static DialogueConfigurationException At(string message, SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(node);
        return new DialogueConfigurationException(message, TomlLocation.From(node.Span));
    }
}
