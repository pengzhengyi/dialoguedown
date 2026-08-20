using System.Collections.Immutable;

namespace DialogueDown.Playbook;

/// <summary>
/// Runs several checks as one, in the order they were given.
/// </summary>
/// <remarks>
/// Order is how a caller says which failure explains the others: there is no point reporting that
/// an edge leads nowhere in a document whose version we could not read in the first place. So the
/// first refusal ends the run.
/// </remarks>
public sealed class CompositeChecker : IPlaybookChecker
{
    private readonly ImmutableArray<IPlaybookChecker> _checkers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeChecker"/> class.
    /// </summary>
    /// <param name="checkers">The checks to run, in the order they are worth asking.</param>
    public CompositeChecker(params IPlaybookChecker[] checkers) =>
        _checkers = [.. checkers.AssertNoneMissing(nameof(checkers))];

    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        foreach (var checker in _checkers)
        {
            checker.Check(playbook);
        }
    }
}
