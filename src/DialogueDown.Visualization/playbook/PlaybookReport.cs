using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Playbook;

/// <summary>
/// The playbook section of the report payload: the compiled playbook as a runtime would receive
/// it, beside the few facts worth reading without scrolling the document.
/// </summary>
/// <remarks>
/// A playbook is the compiler's output artifact, so the report shows it verbatim — the same bytes
/// <c>ddown compile --emit playbook</c> writes. <see cref="Metadata"/> and <see cref="Speakers"/>
/// are not extra information; they are what a reader would otherwise hunt for at the top and
/// bottom of a long JSON file.
/// </remarks>
/// <param name="Json">The playbook, formatted for reading. Null when the compile produced none.</param>
/// <param name="Metadata">The document's header facts, for the summary table.</param>
/// <param name="Speakers">Every speaker the playbook declares.</param>
/// <param name="Anchors">Every anchor a jump may name, with the node it lands on.</param>
/// <param name="Nodes">Every node the playbook holds, in document order.</param>
/// <param name="Unavailable">Why there is no playbook, or null when there is one.</param>
internal sealed record PlaybookReport(
    string? Json,
    PlaybookMetadataView? Metadata,
    IReadOnlyList<PlaybookSpeakerView> Speakers,
    IReadOnlyList<PlaybookAnchorView> Anchors,
    IReadOnlyList<PlaybookNodeView> Nodes,
    string? Unavailable);

/// <summary>One anchor a jump may name, and where it lands.</summary>
/// <param name="Name">The anchor's slug, as a jump writes it.</param>
/// <param name="Node">The node position the anchor resolves to.</param>
internal sealed record PlaybookAnchorView(string Name, int Node);

/// <summary>One node as the table shows it.</summary>
/// <param name="Id">The node's own position in the playbook.</param>
/// <param name="Kind">The kind the document names the node by.</param>
/// <param name="Category">The category that decides what color the node draws in.</param>
/// <param name="Summary">The one line saying what the node holds.</param>
/// <param name="Segments">That line, as the labeled pieces the summary cell draws.</param>
/// <param name="Targets">The nodes this one leads to, in the order they appear.</param>
internal sealed record PlaybookNodeView(
    int Id,
    string Kind,
    string Category,
    string Summary,
    IReadOnlyList<PlaybookSegmentView> Segments,
    IReadOnlyList<int> Targets);

/// <summary>One labeled piece of a node's summary.</summary>
/// <param name="Text">The piece's text. Joining every piece's text rebuilds the summary.</param>
/// <param name="Role">
/// What the piece is — a speaker, a writer's words, the table's grammar, a command, a query, or an
/// absent marker — named by <see cref="PlaybookNodeSummary"/> as it writes the piece.
/// </param>
internal sealed record PlaybookSegmentView(string Text, string Role);

/// <summary>The playbook's header facts, as the report's summary table shows them.</summary>
/// <param name="Script">The script the playbook was compiled from.</param>
/// <param name="FormatVersion">The playbook format's version.</param>
/// <param name="SchemaUrl">Where the format this playbook was written to is published.</param>
/// <param name="Requires">Capabilities a runtime must support to play this playbook.</param>
/// <param name="Uses">Capabilities this playbook actually uses.</param>
/// <param name="Entry">The node a playthrough starts at.</param>
/// <param name="NodeCount">How many nodes the playbook holds.</param>
/// <param name="AnchorCount">How many named anchors a jump can target.</param>
internal sealed record PlaybookMetadataView(
    string Script,
    int FormatVersion,
    string SchemaUrl,
    IReadOnlyList<string> Requires,
    IReadOnlyList<string> Uses,
    int Entry,
    int NodeCount,
    int AnchorCount);

/// <summary>One speaker as the playbook declares it.</summary>
/// <param name="Id">The speaker's stable id, or null when it has none.</param>
/// <param name="Name">The speaker's display name, or null for an anonymous speaker.</param>
/// <param name="Default">Whether unattributed lines belong to this speaker.</param>
/// <param name="Tags">The speaker's tags, structured so the report can draw each as a capsule.</param>
internal sealed record PlaybookSpeakerView(
    string? Id,
    string? Name,
    bool Default,
    IReadOnlyList<TagView> Tags);
