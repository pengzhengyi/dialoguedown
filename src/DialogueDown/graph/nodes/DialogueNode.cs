using DialogueDown.Common;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A node in the dialogue graph — a unit of flow identified by its <see cref="Id"/>, spanning the
/// source it was lowered from, with the edges leaving it in <see cref="Out"/>. The sealed
/// hierarchy names each kind the builder emits. The <see cref="Span"/> is what lets a tool point
/// back at the script — a debugger highlighting where a run has paused, an inspector revealing
/// the node a reader clicked — since the graph is otherwise a closed artifact with no route home.
/// </summary>
internal abstract record DialogueNode(NodeId Id, SourceSpan Span, IReadOnlyList<Edge> Out);
