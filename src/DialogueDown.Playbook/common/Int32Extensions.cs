namespace DialogueDown.Playbook.Common;

/// <summary>
/// Guards for the whole numbers a playbook carries — node indices and the order branch arms are
/// tried — where a negative value is never a position, only a mistake.
/// </summary>
internal static class Int32Extensions
{
    /// <summary>
    /// The number itself, or an <see cref="ArgumentOutOfRangeException"/> when it is negative.
    /// </summary>
    /// <remarks>Zero passes: the first node and the first branch arm both sit at zero.</remarks>
    /// <param name="value">The number to check.</param>
    /// <param name="paramName">The name reported on the exception.</param>
    /// <returns>The same number, so a caller can assign in one expression.</returns>
    public static int AssertNotNegative(this int value, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
        return value;
    }
}
