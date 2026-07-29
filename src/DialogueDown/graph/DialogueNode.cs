namespace DialogueDown.Graph;

/// <summary>
/// A node in the dialogue graph — a unit of flow identified by its <see cref="Id"/>, with the
/// edges leaving it in <see cref="Out"/>. The sealed hierarchy names each kind the builder emits.
/// </summary>
internal abstract record DialogueNode(NodeId Id, IReadOnlyList<Edge> Out);
