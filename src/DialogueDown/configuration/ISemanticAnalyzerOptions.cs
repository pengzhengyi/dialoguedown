namespace DialogueDown.Configuration;

/// <summary>
/// The options the semantic analysis stage reads, taken from <see cref="CompilerOptions"/> so the
/// analyzer depends only on the options it uses.
/// </summary>
internal interface ISemanticAnalyzerOptions
{
    /// <summary>The speakers supplied by configuration, seeded alongside a script's own speakers.</summary>
    IReadOnlyList<ConfiguredSpeaker> ConfiguredSpeakers { get; }
}
