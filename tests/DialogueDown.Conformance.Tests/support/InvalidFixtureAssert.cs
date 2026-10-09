namespace DialogueDown.Conformance.Tests.Support;

/// <summary>Assertions about a fixture, case, or corpus file the conformance reader refuses.</summary>
internal static class InvalidFixtureAssert
{
    /// <summary>Asserts that <paramref name="read"/> refuses, with a message that mentions each fragment.</summary>
    /// <param name="read">The read that must refuse.</param>
    /// <param name="mentioning">Text the message must contain.</param>
    /// <returns>The error, for a test that says more about it.</returns>
    public static InvalidFixtureException AssertInvalid(Action read, params string[] mentioning)
    {
        var error = Assert.Throws<InvalidFixtureException>(read);
        foreach (var fragment in mentioning)
        {
            Assert.Contains(fragment, error.Message, StringComparison.Ordinal);
        }

        return error;
    }
}
