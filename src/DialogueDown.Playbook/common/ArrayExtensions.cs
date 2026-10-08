namespace DialogueDown.Playbook.Common;

/// <summary>
/// Checks for arrays a caller builds in code, where a <c>null</c> element is a programming error.
/// </summary>
internal static class ArrayExtensions
{
    /// <summary>
    /// The array itself, or an exception when the array or any element of it is <c>null</c>, so
    /// the mistake is reported where the array is built rather than where it is later used.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The array to check.</param>
    /// <param name="paramName">The name reported on the exception.</param>
    /// <returns>The same array, so a caller can assign in one expression.</returns>
    public static T[] AssertNoneMissing<T>(this T[]? values, string paramName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, paramName);

        return Array.IndexOf(values, null) >= 0
            ? throw new ArgumentException(
                $"The {typeof(T).Name} array must not hold a missing element.", paramName)
            : values;
    }
}
