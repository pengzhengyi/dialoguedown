using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A node in the dialogue graph — a unit of flow identified by its <see cref="Id"/>, spanning the
/// source it was lowered from, with the edges leaving it in <see cref="Out"/>. The sealed
/// hierarchy names each kind the builder emits. The <see cref="Span"/> lets a tool point from a
/// node back to the script text it came from.
/// </summary>
internal abstract record DialogueNode(NodeId Id, SourceSpan Span, IReadOnlyList<Edge> Out);
