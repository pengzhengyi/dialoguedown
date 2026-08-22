using DialogueDown.Playbook.Weights;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Emission;

/// <summary>
/// Writes how often a random arm should be taken.
/// </summary>
/// <remarks>
/// A weight is written as an object with a kind rather than as a bare number, because two of the
/// three kinds have no number to write: an automatic share depends on the arms offered alongside
/// it, and a queried one is whatever the game says at the moment of asking.
/// </remarks>
internal static class WeightMapping
{
    /// <summary>Writes one weight.</summary>
    /// <param name="weight">The share to write.</param>
    /// <returns>The same weight as a playbook carries it.</returns>
    public static ChoiceWeight Write(Ast.ChoiceWeight weight)
    {
        ArgumentNullException.ThrowIfNull(weight);

        return weight switch
        {
            Ast.NumberWeight number => new NumberWeight(number.Percentage),
            Ast.AutoWeight => new AutoWeight(),
            Ast.QueryWeight query => new QueryWeight(query.Key),
            _ => throw new NotSupportedException(
                $"No playbook weight is defined for {weight.GetType().Name}."),
        };
    }
}
