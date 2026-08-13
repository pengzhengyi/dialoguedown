using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader;

/// <summary>
/// Creates configuration errors at the TOML syntax node that violated the schema. Readers supply
/// the domain-specific message; this helper keeps the Tomlyn-span to public-location mapping in
/// one place.
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
