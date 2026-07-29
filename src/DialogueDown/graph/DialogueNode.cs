namespace DialogueDown.Graph;

/// <summary>
/// A node in the dialogue graph — a unit of flow identified by its <see cref="Id"/>. The sealed
/// hierarchy names each kind the builder emits; each kind lives in its own file so adding one
/// (a line, a choice, a future start node) never touches the others.
/// </summary>
internal abstract record DialogueNode(NodeId Id);
