using DialogueDown.Visualization.Configuration;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// Drives the served <c>dialoguedown visualize</c> experience: one loopback server that hosts the
/// report shell with its Explorer sidebar, confined to a root. Given a <c>script</c> it opens that
/// document's report directly (resolving the served root from the script, with consent when it
/// references images above its folder); with no script it lands on the empty shell so a script can
/// be browsed or created in the tree. Runs until canceled. Injected so the command is testable with
/// a substitute.
/// </summary>
public interface IServedShellRunner
{
    /// <summary>
    /// Serves the report shell on a loopback port, opening the browser unless
    /// <paramref name="noOpen"/>. When <paramref name="script"/> is given, the run opens that
    /// document's report and hosts the served root resolved from it (pinned by
    /// <paramref name="root"/> when set); otherwise it serves the empty shell rooted at
    /// <paramref name="root"/> (the current directory when null). Every report the shell opens
    /// shows the applied <paramref name="configuration"/>. <paramref name="mode"/> is the initial
    /// side of the View/Edit toggle. Runs until <paramref name="cancellationToken"/> is canceled.
    /// Returns a process exit code.
    /// </summary>
    Task<int> RunAsync(
        string? script,
        string? root,
        ReportMode mode,
        int? port,
        bool noOpen,
        AppliedConfiguration configuration,
        CancellationToken cancellationToken);
}
