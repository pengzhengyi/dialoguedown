using System.Collections.Immutable;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a playbook that needs a construct this build does not understand.
/// </summary>
/// <remarks>
/// Only what a playbook requires is checked. What it merely uses is advisory — extra detail a
/// reader may ignore and still play the story correctly — so an unknown name there passes.
/// </remarks>
public sealed class CapabilityChecker : IPlaybookChecker
{
    private readonly ImmutableHashSet<string> _understood;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityChecker"/> class.
    /// </summary>
    /// <param name="understood">The capabilities this reader honors. Matched exactly.</param>
    public CapabilityChecker(IEnumerable<string> understood)
    {
        ArgumentNullException.ThrowIfNull(understood);

        _understood = [.. understood];
    }

    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        foreach (var capability in playbook.Format.Requires)
        {
            if (!_understood.Contains(capability))
            {
                throw new InvalidPlaybookException(
                    $"This playbook requires '{capability}', which this build does not understand.");
            }
        }
    }
}
