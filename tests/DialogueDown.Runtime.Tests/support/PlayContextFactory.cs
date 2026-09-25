using System.Collections.Immutable;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Playbooks built by hand, so a test about stepping says what it is about rather than compiling a
/// script to get there.
/// </summary>
/// <remarks>
/// The nodes come from <see cref="PlaybookNodes"/>; this builds the document around them and the
/// context that steps it. A test that needs the document itself rather than a context over it —
/// the writer's tests, say — asks for <see cref="Document"/>.
/// </remarks>
internal static class PlayContextFactory
{
    /// <summary>A context over exactly the nodes and speakers a test names.</summary>
    /// <param name="nodes">The steps of the playthrough.</param>
    /// <param name="speakers">Everybody who speaks, by the name they speak under; a null name is the anonymous default speaker.</param>
    /// <param name="entry">Where a playthrough begins.</param>
    /// <returns>A context ready to step.</returns>
    public static PlayContext Of(
        ImmutableArray<Node> nodes, IEnumerable<string?>? speakers = null, int entry = 0) =>
        PlayContext.Of(Document(nodes, [.. (speakers ?? []).Select(Speaker)], entry));

    /// <summary>The document behind such a context, for a test that needs the playbook itself.</summary>
    /// <param name="nodes">The steps of the playthrough.</param>
    /// <param name="speakers">Everybody who speaks.</param>
    /// <param name="entry">Where a playthrough begins.</param>
    /// <returns>The playbook.</returns>
    public static PlaybookDocument Document(
        ImmutableArray<Node> nodes, ImmutableArray<PlaybookSpeaker> speakers, int entry = 0) =>
        new(
            new PlaybookFormat(PlaybookSupport.NewestReadableVersion, ["core"], []),
            script: "a-script.dialogue.md",
            entry: entry,
            anchors: ImmutableSortedDictionary<string, int>.Empty,
            speakers: speakers,
            nodes: nodes);

    /// <summary>One line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context a single <c>Next</c> finishes.</returns>
    public static PlayContext OneLine() =>
        Of([Line(0, speaker: 0, "Hello.", next: 1), End(1)], ["Alice"]);

    /// <summary>Two lines, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello.
    ///
    /// Bob: Goodbye.
    /// </code>
    /// </remarks>
    /// <returns>A context that shows succession going somewhere.</returns>
    public static PlayContext TwoLines() =>
        Of(
            [
                Line(0, speaker: 0, "Hello.", next: 1),
                Line(1, speaker: 1, "Goodbye.", next: 2),
                End(2),
            ],
            ["Alice", "Bob"]);

    /// <summary>A choice, which is a node kind this pass cannot play.</summary>
    /// <remarks>
    /// <code>
    /// - Go east
    /// </code>
    /// </remarks>
    /// <returns>A context that begins at a choice.</returns>
    public static PlayContext NotYetPlayable() =>
        Of([Choice(0, leadsTo: 1), End(1)], ["Alice"]);

    /// <summary>A ring of jumps, each leading to the next and the last back to the first.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -> node 1 -> node 2 -> ... -> node length-1 -> node 0
    /// </code>
    /// Nothing in it ever hands the host anything, so a walk with no bound never comes out.
    /// </remarks>
    /// <param name="length">How many jumps the ring holds.</param>
    /// <returns>A context whose entry walks forever unless something stops it.</returns>
    public static PlayContext RingOfJumps(int length) =>
        Of([.. Enumerable.Range(0, length).Select(at => Jump(at, (at + 1) % length))]);

    /// <summary>A chain of jumps ending at the end.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -> node 1 -> node 2 -> ... -> node jumps-1 -> node jumps, the end
    /// </code>
    /// The walk passes every node the playbook has, exactly once. One node further along and it
    /// would be passing one of them twice, which is the case a bound must not confuse this with.
    /// </remarks>
    /// <param name="jumps">How many jumps precede the end.</param>
    /// <returns>A context whose walk passes every node exactly once.</returns>
    public static PlayContext ChainOfJumps(int jumps) =>
        Of(
            [
                .. Enumerable.Range(0, jumps).Select(at => (Node)Jump(at, at + 1)),
                End(jumps),
            ]);

    /// <summary>An effect, then a line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `("fade in")`
    ///
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context whose run waits on the host before it says anything.</returns>
    public static PlayContext AnEffectThenALine() =>
        Of(
            [
                Effects(0, next: 1, "fade in"),
                Line(1, speaker: 0, "Hello.", next: 2),
                End(2),
            ],
            ["Alice"]);

    /// <summary>A line whose jump the world must allow, with a line to fall through to.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Away. `Alice.HasKey?` =&gt; [Inside](#inside)
    ///
    /// Alice: Here.
    ///
    /// # Inside
    ///
    /// Alice: Inside.
    /// </code>
    /// </remarks>
    /// <returns>A context where the world decides which line is said after the first.</returns>
    public static PlayContext ALineWhoseJumpAsksTheWorld() =>
        Of(
            [
                LineWithConditionalJump(0, speaker: 0, "Away.", jumpTo: 2, next: 1, key: "Alice.HasKey"),
                Line(1, speaker: 0, "Here.", next: 2),
                Line(2, speaker: 0, "Inside.", next: 3),
                End(3),
            ],
            ["Alice"]);

    /// <summary>A jump on its own line that the world must allow, with a line to fall through to.</summary>
    /// <remarks>
    /// <code>
    /// `Rainy?` =&gt; [Inn](#inn)
    ///
    /// Alice: Onward in the sun.
    ///
    /// # Inn
    ///
    /// Alice: Inside, out of the rain.
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry says nothing and cannot be left without asking.</returns>
    public static PlayContext AGuardedJumpOnItsOwnLine() =>
        Of(
            [
                Bare(0, Divert(2, "Rainy"), new SuccessionEdge(1)),
                Line(1, speaker: 0, "Onward in the sun.", next: 2),
                Line(2, speaker: 0, "Inside, out of the rain.", next: 3),
                End(3),
            ],
            ["Alice"]);

    /// <summary>A block condition with an if, an elseif, and an else.</summary>
    /// <remarks>
    /// <code>
    /// &gt; `if` `Alice.HasKey?`
    /// &gt;
    /// &gt; Alice: The key turns.
    /// &gt;
    /// &gt; `elseif` `Alice.HasPick?`
    /// &gt;
    /// &gt; Alice: The pick clicks.
    /// &gt;
    /// &gt; `else`
    /// &gt;
    /// &gt; Alice: The door stays shut.
    /// </code>
    /// </remarks>
    /// <returns>A context whose entry says nothing, and whose every arm says a different line.</returns>
    public static PlayContext AConditionalBlock() =>
        Of(
            [
                Branch(0, Arm(1, order: 0, "Alice.HasKey"), Arm(2, order: 1, "Alice.HasPick"), Else(3, order: 2)),
                Line(1, speaker: 0, "The key turns.", next: 4),
                Line(2, speaker: 0, "The pick clicks.", next: 4),
                Line(3, speaker: 0, "The door stays shut.", next: 4),
                End(4),
            ],
            ["Alice"]);

    private static PlaybookSpeaker Speaker(string? name) =>
        new(Id: null, Name: name, Default: name is null, Tags: []);
}
