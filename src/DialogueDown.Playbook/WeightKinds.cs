namespace DialogueDown.Playbook;

/// <summary>
/// The <c>kind</c> values that tag a random option's weight on the wire.
/// </summary>
public static class WeightKinds
{
    /// <summary>An even share of whatever the weighted options leave.</summary>
    public const string Auto = "auto";

    /// <summary>A percentage the writer fixed.</summary>
    public const string Number = "number";

    /// <summary>A percentage the world supplies at play time.</summary>
    public const string Query = "query";
}
