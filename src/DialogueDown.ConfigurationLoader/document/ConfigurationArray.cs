namespace DialogueDown.ConfigurationLoader.Document;

/// <summary>An array read from a configuration file, holding its items in document order.</summary>
/// <remarks>
/// <c>tags = ["main", 42]</c> is an array, and so are the tables of all the <c>[[speakers]]</c>
/// headers together.
/// </remarks>
internal sealed record ConfigurationArray(
    IReadOnlyList<ConfigurationValue> Items, ConfigurationSourceLocation Location)
    : ConfigurationValue(Location);
