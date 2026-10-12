namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>
/// The wording of a reason more than one check reports, so the pre-flight scan of a fixture and the
/// run itself cannot describe one gap in two ways.
/// </summary>
internal static class SessionReasons
{
    /// <summary>A node kind this build cannot play.</summary>
    /// <param name="kind">The kind's name, as the type gives it.</param>
    /// <returns>The reason.</returns>
    public static string UnplayableNodeKind(string kind) => $"nothing plays a {kind} yet";

    /// <summary>A claim in an expectation nothing knows how to check.</summary>
    /// <param name="claim">The claim's key, as a fixture writes it.</param>
    /// <returns>The reason.</returns>
    public static string UncheckableClaim(string claim) => $"nothing checks {claim} yet";

    /// <summary>A command no reader owns.</summary>
    /// <param name="command">The command a send names, or its message when it names none.</param>
    /// <returns>The reason.</returns>
    public static string UnsendableCommand(string command) => $"nothing sends {command} yet";
}
