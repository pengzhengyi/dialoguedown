namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>What became of a session.</summary>
internal enum SessionVerdict
{
    /// <summary>The runner's replies conformed to the whole conversation.</summary>
    Conformed,

    /// <summary>The runner said something the session did not expect.</summary>
    Diverged,

    /// <summary>The session uses something this build has not learned.</summary>
    NotYetRunnable,
}
