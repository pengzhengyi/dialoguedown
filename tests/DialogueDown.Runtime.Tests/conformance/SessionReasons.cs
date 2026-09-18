namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// The wording of a reason more than one check reports, so the pre-flight scan of a fixture and the
/// run itself cannot describe one gap in two ways.
/// </summary>
internal static class SessionReasons
{
    /// <summary>A node kind this build cannot play.</summary>
    /// <param name="construct">The kind's name, as the type gives it.</param>
    /// <returns>The reason.</returns>
    public static string NothingPlays(string construct) => $"nothing plays a {construct} yet";

    /// <summary>A message nothing knows how to send.</summary>
    /// <param name="message">What the session sends, as the fixture writes it.</param>
    /// <returns>The reason.</returns>
    public static string NothingSends(string message) => $"nothing sends {message} yet";
}
