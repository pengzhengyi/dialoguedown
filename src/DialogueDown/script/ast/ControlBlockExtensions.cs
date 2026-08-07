namespace DialogueDown.Script.Ast;

/// <summary>
/// Queries over a <see cref="ControlBlock"/> that read it as a group of arms, the shape it shares
/// with a <see cref="ChoiceGroup"/>.
/// </summary>
internal static class ControlBlockExtensions
{
    /// <summary>The body each branch plays, in the order the branches are tried.</summary>
    public static IReadOnlyList<IReadOnlyList<ScriptBlock>> BranchBodies(this ControlBlock block) =>
        [.. block.Branches.Select(branch => branch.Body)];
}
