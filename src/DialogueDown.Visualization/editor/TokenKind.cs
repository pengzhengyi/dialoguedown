namespace DialogueDown.Visualization.Editor;

/// <summary>
/// The vocabulary of dialogue tokens the editor highlights — the semantic-tokens "legend". Each
/// value names a dialogue-specific construct that generic Markdown highlighting does not
/// understand; a future language server publishes this set as its semantic-tokens legend. The
/// members serialize by name (PascalCase) into the report payload, matching the other payload
/// enums.
/// </summary>
internal enum TokenKind
{
    /// <summary>A speaker's display name (<c>Alice</c>, or a quoted <c>"Dr. Vale"</c>).</summary>
    SpeakerName,

    /// <summary>A speaker's stable id, including its leading <c>@</c> (<c>@alice</c>).</summary>
    SpeakerId,

    /// <summary>The <c>:</c> that ends a speaker prefix, separating it from the speech.</summary>
    Separator,

    /// <summary>A custom tag, including the <c>#</c> and any <c>=value</c> (<c>#happy</c>).</summary>
    CustomTag,

    /// <summary>A reserved tag, including the <c>##</c> (<c>##narrator</c>).</summary>
    ReservedTag,

    /// <summary>The <c>=&gt;</c> that marks a jump.</summary>
    JumpIndicator,
}
