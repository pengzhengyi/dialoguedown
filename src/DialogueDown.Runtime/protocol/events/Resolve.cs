using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Asks the host for the current answers to these keys; the run waits for a <see cref="Supply"/>.
/// </summary>
/// <remarks>
/// Each request names every key one moment at a node needs. Before a node plays, it names the key
/// guarding the node and the keys in its first words: a line reading
/// <c>`Alice.HasKey?` Alice: Hello, `"playerName"`.</c> produces one request naming both. Before a
/// line continues after a command, it names the keys in the words that follow. Before a run leaves
/// a node, it names the keys guarding its ways out: every arm of a block condition, though only
/// the first arm that holds is taken.
/// <para>
/// So a request can name a key whose answer ends up deciding nothing. Answering a key must not
/// change the world. The keys may be answered in any order, one at a time or all at once; the run
/// reads them only when the <see cref="Supply"/> carrying every answer arrives.
/// </para>
/// </remarks>
/// <param name="Keys">The keys the run needs answered, named as the playbook names them.</param>
public sealed record Resolve(ImmutableArray<string> Keys) : Request;
