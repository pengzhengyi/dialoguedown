namespace DialogueDown.Playbook.Common;

/// <summary>
/// Checks for the strings a playbook carries, so a null or empty one is refused when the value is
/// built rather than showing up later as a missing link target or an unnamed speaker.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// The string itself, or an <see cref="ArgumentNullException"/> when it is absent.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="paramName">The name reported on the exception.</param>
    /// <returns>The same string, so a caller can assign in one expression.</returns>
    public static string AssertNotNull(this string? value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    /// The string itself, or an exception when it is absent or empty — for values that may not be
    /// empty, such as the words of a text fragment.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="paramName">The name reported on the exception.</param>
    /// <returns>The same string, so a caller can assign in one expression.</returns>
    public static string AssertNotEmpty(this string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
        return value;
    }
}
