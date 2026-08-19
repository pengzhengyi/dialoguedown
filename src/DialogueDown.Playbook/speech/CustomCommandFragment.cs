using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// A named command with arguments, which the host binds and performs.
/// </summary>
/// <param name="Name">The command's name, as the host binds it.</param>
/// <param name="Args">The arguments the writer supplied. May be empty.</param>
public sealed record CustomCommandFragment(string Name, ImmutableArray<string> Args)
    : SpeechFragment
{
    /// <summary>
    /// Gets the command's name, as the host binds it.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; } = Name.AssertNotNull(nameof(Name));

    /// <summary>
    /// Gets the arguments the writer supplied.
    /// </summary>
    /// <remarks>Empty is valid: a command need not take arguments.</remarks>
    [JsonPropertyName("args")]
    public ImmutableArray<string> Args { get; } = Args.OrEmpty();
}
