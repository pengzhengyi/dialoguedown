namespace DialogueDown.Playbook.Common;

/// <summary>
/// Guards for the arrays a caller composes by hand, where a gap is a wiring mistake rather than
/// a document that says nothing.
/// </summary>
internal static class ArrayExtensions
{
    /// <summary>
    /// The array itself, or an exception when the array or any element of it is missing — so a
    /// gap is reported where it was wired rather than where it is later used.
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
