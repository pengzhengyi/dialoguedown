namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>The kind of branch a block-conditional marker opens.</summary>
internal enum BranchKind
{
    /// <summary>An <c>`if`</c> branch — the first branch, guarded by a condition.</summary>
    If,

    /// <summary>An <c>`elseif`</c> branch — a later branch, also guarded by a condition.</summary>
    ElseIf,

    /// <summary>The <c>`else`</c> branch — the unconditional fallback.</summary>
    Else,
}
