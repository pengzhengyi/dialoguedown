namespace DialogueDown.ConfigurationLoader.Errors;

/// <summary>
/// A project's configuration is malformed. It carries the <see cref="Location"/> of the offending
/// text — a TOML syntax error or a schema violation — so a caller can point at the exact spot. It
/// stands alone rather than joining the core's compilation-error hierarchy, because a configuration
/// fault happens at the edge, before any script is compiled.
/// </summary>
public sealed class DialogueConfigurationException : Exception
{
    public DialogueConfigurationException(string message, ConfigurationSourceLocation location)
        : base(message) => Location = location;

    /// <summary>Where the problem sits in the configuration source.</summary>
    public ConfigurationSourceLocation Location { get; }
}
