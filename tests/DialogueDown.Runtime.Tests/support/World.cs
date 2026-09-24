using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// What the world answers when a run asks it something, written the way a test means it.
/// </summary>
/// <remarks>
/// A supply is a dictionary of answers, so an unhelped test spends a line building one before it
/// states anything. These name the answers instead: a yes or no for a guard, words for a query.
/// </remarks>
internal static class World
{
    /// <summary>What the world answers when it answers nothing at all.</summary>
    /// <returns>An empty supply.</returns>
    public static Supply Answering() => new(ImmutableDictionary<string, Answer>.Empty);

    /// <summary>What the world answers, as a yes or no for each key it was asked about.</summary>
    /// <param name="answers">Each key, with whether it holds.</param>
    /// <returns>The supply.</returns>
    public static Supply Answering(params (string Key, bool Holds)[] answers) =>
        Answering([.. answers.Select(answer => (answer.Key, Answer: (Answer)new BooleanAnswer(answer.Holds)))]);

    /// <summary>What the world answers, as words for each key it was asked about.</summary>
    /// <param name="answers">Each key, with the words that answer it.</param>
    /// <returns>The supply.</returns>
    public static Supply Answering(params (string Key, string Words)[] answers) =>
        Answering([.. answers.Select(answer => (answer.Key, Answer: (Answer)new TextAnswer(answer.Words)))]);

    /// <summary>What the world answers, when the keys it was asked about need answers of both kinds.</summary>
    /// <param name="answers">Each key, with its answer.</param>
    /// <returns>The supply.</returns>
    public static Supply Answering(params (string Key, Answer Answer)[] answers) =>
        new(answers.ToImmutableDictionary(
            answer => answer.Key, answer => answer.Answer, StringComparer.Ordinal));
}
