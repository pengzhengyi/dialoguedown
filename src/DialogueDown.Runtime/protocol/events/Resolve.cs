using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Say what the world makes of these keys, and the run will read on.
/// </summary>
/// <remarks>
/// Everything one node needs is asked for at once: the key guarding the node, the keys guarding
/// its ways out, and the keys standing in what it says. A line reading
/// <c>`Alice.HasKey?` Alice: Hello, `"playerName"`.</c> produces one request naming both keys,
/// not two requests naming one each.
/// <para>
/// Asking together is what lets the whole node be judged against a single reading of the world,
/// so a menu cannot offer one option and hold back another on the same key. It also means a run
/// stops at most once at any node.
/// </para>
/// </remarks>
/// <param name="Keys">The keys the run needs answered, named as the playbook names them.</param>
public sealed record Resolve(ImmutableArray<string> Keys) : Request;
