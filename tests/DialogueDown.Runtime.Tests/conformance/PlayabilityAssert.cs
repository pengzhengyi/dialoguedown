using System.Collections.Immutable;
using DialogueDown.Conformance;

namespace DialogueDown.Runtime.Tests.Conformance;

/// <summary>What this build can play, asserted together with the words its reasons use.</summary>
/// <remarks>
/// Mirrors <see cref="SessionOutcomeAssert"/>: asking about playability answers with reasons, so
/// these assert the fact and the wording in one call rather than leaving every test to spell out
/// both. Each asserts the reasons exactly, in the order asked, because the order is deterministic
/// and a changed wording should fail a test.
/// </remarks>
internal static class PlayabilityAssert
{
    /// <summary>Asserts this build can take the session entry.</summary>
    /// <param name="entry">The entry to ask about.</param>
    public static void AssertPlayable(SessionEntry entry) =>
        Assert.True(Playability.CanPlay(entry));

    /// <summary>Asserts this build cannot take the session entry.</summary>
    /// <param name="entry">The entry to ask about.</param>
    public static void AssertNotPlayable(SessionEntry entry) =>
        Assert.False(Playability.CanPlay(entry));

    /// <summary>Asserts this build cannot take the entry, saying exactly these things.</summary>
    /// <param name="entry">The entry to ask about.</param>
    /// <param name="reasons">The reasons it should give, in order.</param>
    public static void AssertNotPlayable(SessionEntry entry, params string[] reasons)
    {
        Assert.False(Playability.CanPlay(entry));
        Assert.Equal(reasons, Playability.WhyNotPlayable(entry));
    }

    /// <summary>Asserts the playbook is not playable yet, saying exactly these things.</summary>
    /// <param name="context">The playbook to ask about.</param>
    /// <param name="reasons">The reasons it should give, in order.</param>
    public static void AssertNotPlayable(PlayContext context, params string[] reasons) =>
        Assert.Equal(reasons, Playability.WhyNotPlayable(context));

    /// <summary>Asserts the session is not playable yet, saying exactly these things.</summary>
    /// <param name="session">The session to ask about.</param>
    /// <param name="reasons">The reasons it should give, in order.</param>
    public static void AssertNotPlayable(ImmutableArray<SessionEntry> session, params string[] reasons) =>
        Assert.Equal(reasons, Playability.WhyNotPlayable(session));
}
