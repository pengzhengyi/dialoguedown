namespace DialogueDown.Graph;

/// <summary>
/// A node in the dialogue graph — a unit of flow identified by its <see cref="Id"/>. The sealed
/// hierarchy names each kind the builder emits.
/// </summary>
internal abstract record DialogueNode(NodeId Id);
