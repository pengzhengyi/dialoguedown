namespace DialogueDown.ConfigurationLoader.Document;

/// <summary>An array read from a configuration file, holding its items in document order.</summary>
/// <remarks>
/// <c>tags = ["main", 42]</c> and the entries of every <c>[[speakers]]</c> header are both arrays.
/// </remarks>
internal sealed record ConfigurationArray(
    IReadOnlyList<ConfigurationValue> Items, ConfigurationSourceLocation Location)
    : ConfigurationValue(Location);
