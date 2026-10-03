using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Maps each analyzed <c>Jump</c> to what it resolved to. Every jump in the analyzed tree is
/// present, so <see cref="Resolve"/> treats a missing jump as a caller error.
/// </summary>
internal sealed class JumpResolutionTable
{
    private readonly IReadOnlyDictionary<Jump, JumpResolution> _resolutionByJump;

    public JumpResolutionTable(IReadOnlyDictionary<Jump, JumpResolution> resolutionByJump)
    {
        ArgumentNullException.ThrowIfNull(resolutionByJump);
        _resolutionByJump = resolutionByJump;
    }

    /// <summary>How many jumps the table holds.</summary>
    public int Count => _resolutionByJump.Count;

    /// <summary>Every resolution in the table, for a consumer that walks them all.</summary>
    public IEnumerable<JumpResolution> Resolutions => _resolutionByJump.Values;

    /// <summary>What <paramref name="jump"/> resolved to; throws if it was not analyzed.</summary>
    public JumpResolution Resolve(Jump jump) =>
        _resolutionByJump.TryGetValue(jump, out var resolution)
            ? resolution
            : throw new ArgumentException(
                "This jump was not analyzed, so it has no resolution in this table.", nameof(jump));
}
