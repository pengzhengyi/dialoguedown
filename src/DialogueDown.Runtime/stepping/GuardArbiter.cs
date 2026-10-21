using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Excludes a random option from the draw when the world withholds its guard.
/// </summary>
/// <remarks>
/// A random option nobody guarded is always in play, so judging it needs no answers. A guarded one
/// is judged by the world's answers: <c>`Guard.IsAngry?`</c> answered
/// <c>{ "Guard.IsAngry": false }</c> excludes the random option it guards.
/// </remarks>
internal sealed class GuardArbiter : IRandomOptionArbiter
{
    private readonly Supply? _supply;

    /// <summary>Initializes a new instance of the <see cref="GuardArbiter"/> class.</summary>
    /// <param name="supply">What the world said, or <see langword="null"/> when it was asked nothing.</param>
    public GuardArbiter(Supply? supply) => _supply = supply;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// The random option is guarded, and the world was asked nothing.
    /// </exception>
    public bool Excludes(RandomOptionEdge option)
    {
        ArgumentNullException.ThrowIfNull(option);

        if (option.Condition is null)
        {
            return false;
        }

        return _supply is null
            ? throw new InvalidOperationException(
                "A guarded random option is judged by the world's answers, and the world was asked "
                    + "nothing. A random choice's shares are worked out with a guard in them only once "
                    + "the world has answered, so reaching this means that check was skipped.")
            : !_supply.Allows(option);
    }
}
