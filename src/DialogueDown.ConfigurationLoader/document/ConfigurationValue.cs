namespace DialogueDown.ConfigurationLoader.Document;

/// <summary>A value read from a configuration file, with the location it was written at.</summary>
/// <remarks>
/// The tree does not depend on the file format: every spelling of a TOML table, whether a
/// header, an inline table, or a dotted key, becomes the same <see cref="ConfigurationTable"/>.
/// </remarks>
internal abstract record ConfigurationValue(ConfigurationSourceLocation Location);
