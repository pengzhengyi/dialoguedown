namespace DialogueDown.Play.Tui;

/// <summary>
/// The player's turn as a value: what they did at a wait. The presentation never sends anything
/// and never decides what a turn means; the driver that owns a run turns these into its protocol's
/// commands.
/// </summary>
public abstract record PlayerTurn
{
    private protected PlayerTurn()
    {
    }

    /// <summary>The player went on: Enter at a line's wait.</summary>
    public sealed record Advance : PlayerTurn;

    /// <summary>The player took the option at <paramref name="Index"/> among those offered.</summary>
    /// <param name="Index">The position of the taken option, as offered.</param>
    public sealed record Take(int Index) : PlayerTurn;

    /// <summary>The player asked to stop, with Ctrl+C.</summary>
    public sealed record Quit : PlayerTurn;
}
