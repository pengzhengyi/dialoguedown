using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// A world drawn beside a playbook, answering every key the playbook can ask about.
/// </summary>
/// <remarks>
/// A key a guard reads is answered with the truth drawn for it. Any other key is one a query reads,
/// and is answered with the same words every time.
/// </remarks>
/// <param name="truths">Whether each key a guard reads holds.</param>
internal sealed class DrawnWorld(ImmutableDictionary<string, bool> truths)
{
    /// <summary>What this world answers when a run asks it about these keys.</summary>
    /// <param name="keys">The keys the run asked about.</param>
    /// <returns>The supply, answering each key once.</returns>
    public Supply Answering(ImmutableArray<string> keys) =>
        World.Answering([.. keys.Select(key => (key, AnswerTo(key)))]);

    private Answer AnswerTo(string key) =>
        truths.TryGetValue(key, out var holds) ? new BooleanAnswer(holds) : new TextAnswer("Robin");
}
