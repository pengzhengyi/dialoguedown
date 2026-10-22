using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Asks the host for the current answers to these keys; the run waits for a <see cref="Supply"/>.
/// </summary>
/// <remarks>
/// Each request names every key one moment at a node needs, each under the kind of answer it
/// needs. Before a node plays, it names the key guarding the node and the keys in its first words:
/// a line reading <c>`Alice.HasKey?` Alice: Hello, `"playerName"`.</c> produces one request naming
/// <c>Alice.HasKey</c> for a truth and <c>playerName</c> for words. A menu names the keys guarding
/// its options and the keys in their labels. A random choice names the keys guarding its random
/// options and the keys their key weights read, so
/// <c>- `Guard.IsAngry?` `Guard.Mood%` Guard: Halt!</c> asks for a truth and a number too. Before
/// a line continues after a command, it names the keys in the words that follow. Before a run
/// leaves a node, it names the keys guarding its ways out: every branch of a block condition,
/// though only the first branch that holds is taken.
/// <para>
/// The runner names a key in one kind only, and once there, in the order the playbook writes it.
/// A node that needs one key as two kinds is refused before anything is asked.
/// </para>
/// <para>
/// So a request can name a key whose answer ends up deciding nothing. Answering a key must not
/// change the world. The keys may be answered in any order, one at a time or all at once; the run
/// reads them only when the <see cref="Supply"/> carrying every answer arrives.
/// </para>
/// </remarks>
/// <param name="BooleanKeys">The keys to answer with a truth, such as a key guarding a line.</param>
/// <param name="TextKeys">The keys to answer with words, such as a query in what a line says.</param>
/// <param name="NumberKeys">The keys to answer with a number, such as a random option's key weight.</param>
public sealed record Resolve(
    ImmutableArray<string> BooleanKeys,
    ImmutableArray<string> TextKeys,
    ImmutableArray<string> NumberKeys) : Request
{
    /// <summary>Every key the run needs answered: the boolean keys, then the text, then the number.</summary>
    public ImmutableArray<string> Keys => [.. BooleanKeys, .. TextKeys, .. NumberKeys];
}
