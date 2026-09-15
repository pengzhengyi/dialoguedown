namespace DialogueDown.Playbook.Tests.Support;

/// <summary>
/// Asserts that two values are one value: equal, and hashing alike.
/// </summary>
/// <remarks>
/// A record derives its <c>==</c> operator from <c>Equals</c>, so pinning <c>Equals</c> and the
/// hash is what an equality test means — and it keeps the operator, the method, and the hash from
/// being spelled out three times at every call site.
/// </remarks>
internal static class EqualityAssert
{
    /// <summary>Asserts two values are equal and hash alike.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="left">One of the two values.</param>
    /// <param name="right">The other, structurally equal to <paramref name="left"/>.</param>
    public static void AssertValueEqual<T>(T left, T right)
        where T : notnull
    {
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>Asserts two values are not equal.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="left">One of the two values.</param>
    /// <param name="right">The other, structurally different from <paramref name="left"/>.</param>
    public static void AssertValueUnequal<T>(T left, T right)
        where T : notnull =>
        Assert.NotEqual(left, right);
}
