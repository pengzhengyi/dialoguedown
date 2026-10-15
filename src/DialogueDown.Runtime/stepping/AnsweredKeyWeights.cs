using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Gives each key weight the number the world supplied for its key.
/// </summary>
/// <remarks>
/// <c>`Guard.Mood%`</c> answered <c>{ "Guard.Mood": 20 }</c> is given <c>20</c>, so its random
/// option is drawn as though <c>`20%`</c> were written in its place.
/// </remarks>
internal sealed class AnsweredKeyWeights : IKeyWeightAnswers
{
    private readonly Supply? _supply;

    /// <summary>Initializes a new instance of the <see cref="AnsweredKeyWeights"/> class.</summary>
    /// <param name="supply">What the world said, or <see langword="null"/> when it was asked nothing.</param>
    public AnsweredKeyWeights(Supply? supply) => _supply = supply;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// The world was asked nothing, or the key went unanswered or was answered with something other
    /// than a number.
    /// </exception>
    public double NumberFor(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _supply is null
            ? throw new InvalidOperationException(
                "A key weight is given the number the world answered, and the world was asked "
                    + "nothing. A random choice's shares are worked out with a key weight in them only "
                    + "once the world has answered, so reaching this means that check was skipped.")
            : _supply.Number(key);
    }
}
