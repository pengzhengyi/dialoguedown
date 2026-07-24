using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// The source locations of a written speaker prefix's parts, kept as distinct sub-spans of the
/// prefix: the display <see cref="Name"/>, the optional <see cref="Id"/> (<c>@id</c>), and the
/// <see cref="Separator"/> (<c>:</c>). A written prefix always ends in a colon, so
/// <see cref="Separator"/> is required; <see cref="Name"/> and <see cref="Id"/> are optional
/// because a prefix may name a speaker by name only or by id only. A speaker with no written
/// prefix — a filled default, or one built from configuration — carries no
/// <see cref="SpeakerPrefixSpans"/> at all.
/// </summary>
internal sealed record SpeakerPrefixSpans(SourceSpan? Name, SourceSpan? Id, SourceSpan Separator);
