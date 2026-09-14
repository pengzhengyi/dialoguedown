namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Names a speaker for a message a contributor will read.</summary>
internal static class SpeakerNames
{
    /// <summary>Names a speaker, including the one a line leaves unnamed.</summary>
    /// <param name="speaker">Who spoke, or <see langword="null"/> for the anonymous default speaker.</param>
    /// <returns>The name, or a stand-in that reads as a placeholder rather than as a name.</returns>
    public static string Of(string? speaker) => speaker ?? "<default>";
}
