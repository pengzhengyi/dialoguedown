using System.Collections.Frozen;

namespace DialogueDown.Configuration;

/// <summary>
/// The closed vocabulary of reserved (<c>##name</c>) tags that DialogueDown owns. A reserved
/// tag is meaningful only when its name is in this set; anything else is rejected. It is the
/// single public source of truth for reserved-tag names, shared by the compiler's passes (the
/// speaker binder on <see cref="Default"/>, the tag validator on <see cref="Known"/>) and any
/// configuration loader that validates a project's reserved keys.
/// </summary>
public static class ReservedTagNames
{
    /// <summary>Marks the speaker that carries it as the document's default speaker.</summary>
    public const string Default = "default";

    /// <summary>Every reserved-tag name DialogueDown recognizes.</summary>
    public static readonly FrozenSet<string> Known = new[] { Default }.ToFrozenSet();
}
