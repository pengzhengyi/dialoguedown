using System.Collections.Immutable;

namespace DialogueDown.Playbook.Common;

/// <summary>
/// Guards for <see cref="ImmutableArray{T}"/>, whose <c>default</c> value wraps no array at
/// all and throws on almost every member — the shape an omitted JSON array deserializes into.
/// </summary>
/// <remarks>
/// The compiler has a sibling of this helper in its own Common layer. The duplication is
/// deliberate: this assembly is the contract a game embeds and therefore references nothing,
/// so sharing four lines would cost the property that keeps a shipped game small.
/// </remarks>
internal static class ImmutableArrayExtensions
{
    /// <summary>
    /// The array itself, or an empty one when it was never initialized — so an omitted JSON
    /// array reads as "none" rather than throwing on first use.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The array to normalize.</param>
    /// <returns>An array that is always safe to enumerate.</returns>
    public static ImmutableArray<T> OrEmpty<T>(this ImmutableArray<T> values) =>
        values.IsDefault ? [] : values;

    /// <summary>
    /// The array itself, or an <see cref="ArgumentException"/> when it holds nothing — for the
    /// places where an empty list is not a valid document, such as styling that wraps no words.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The array to check.</param>
    /// <param name="paramName">The name reported on the exception.</param>
    /// <returns>The same array, so a caller can assign in one expression.</returns>
    public static ImmutableArray<T> AssertNotEmpty<T>(
        this ImmutableArray<T> values, string paramName) =>
        values.IsDefaultOrEmpty
            ? throw new ArgumentException(
                $"The immutable {typeof(T).Name} array must hold at least one element.", paramName)
            : values;
}
