using System.Collections.Immutable;

namespace DialogueDown.Common;

/// <summary>
/// Guards for <see cref="ImmutableArray{T}"/>, whose <c>default</c> value wraps no array at
/// all and throws on almost every member — a trap a reference type does not have.
/// </summary>
internal static class ImmutableArrayExtensions
{
    /// <summary>
    /// The array itself, or an <see cref="ArgumentException"/> when it was never initialized.
    /// </summary>
    /// <remarks>
    /// An empty array passes: the distinction that matters is "no elements" versus "no array at
    /// all", and only the second is a programming error.
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The array to check.</param>
    /// <param name="paramName">The name reported on the exception.</param>
    /// <returns>The same array, so a caller can assign in one expression.</returns>
    public static ImmutableArray<T> AssertInitialized<T>(
        this ImmutableArray<T> values, string paramName) =>
        values.IsDefault
            ? throw new ArgumentException(
                $"The immutable {typeof(T).Name} array must be initialized.", paramName)
            : values;
}
