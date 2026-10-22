using System.Collections.Immutable;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Playbooks built by hand, so a test about stepping says what it is about rather than compiling a
/// script to get there.
/// </summary>
/// <remarks>
/// The nodes come from <see cref="PlaybookNodeFactory"/>; this builds the document around them and
/// the context that steps it. A test that needs the document itself rather than a context over it —
/// the playbook generator, say — asks for <see cref="Document"/>. A few scenarios are single nodes
/// that several test classes share; they return the node rather than a context.
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
        PlayContext.Of(Document(nodes, PlaybookFactory.Speakers([.. speakers ?? []]), entry));

    /// <summary>The document a context is built over, for a test that needs the playbook itself.</summary>
    /// <param name="nodes">The steps of the playthrough.</param>
    /// <param name="speakers">Everybody who speaks.</param>
    /// <param name="entry">Where a playthrough begins.</param>
    /// <returns>The playbook.</returns>
    public static PlaybookDocument Document(
        ImmutableArray<Node> nodes, ImmutableArray<PlaybookSpeaker> speakers, int entry = 0) =>
        PlaybookFactory.Document(
            format: PlaybookFactory.Format(PlaybookSupport.NewestReadableVersion),
            script: "a-script.dialogue.md",
            entry: entry,
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
        Of([Line("Hello.", id: 0, next: 1), End(id: 1)], ["Alice"]);

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
                Line("Hello.", id: 0, next: 1),
                Line("Goodbye.", speaker: 1, id: 1, next: 2),
                End(id: 2),
            ],
            ["Alice", "Bob"]);

    /// <summary>A line with a query standing in what it says, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello, `"playerName"`.
    /// </code>
    /// </remarks>
    /// <returns>A context that cannot say its only line until the world names the player.</returns>
    public static PlayContext ALineWithAQuery() =>
        Of(
            [
                LineOf([Text("Hello, "), Query("playerName"), Text(".")], id: 0, next: 1),
                End(id: 1),
            ],
            ["Alice"]);

    /// <summary>A line ending with a command, then another line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello. `Wave()`
    ///
    /// Bob: Goodbye.
    /// </code>
    /// </remarks>
    /// <returns>A context whose first line waits on the host before the player can move on.</returns>
    public static PlayContext ALineEndingWithACommand() =>
        Of(
            [
                LineOf([Text("Hello. "), Wave()], id: 0, next: 1),
                Line("Goodbye.", speaker: 1, id: 1, next: 2),
                End(id: 2),
            ],
            ["Alice", "Bob"]);

    /// <summary>A line with a query on each side of its command, then another line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hello, `"playerName"`. `Wave()` You look `"mood"`.
    ///
    /// Bob: Goodbye.
    /// </code>
    /// </remarks>
    /// <returns>A context whose first line needs the mood only for the words after its command.</returns>
    public static PlayContext ALineWithAQueryAfterACommand() =>
        Of(
            [
                LineOf(
                    [
                        Text("Hello, "),
                        Query("playerName"),
                        Text(". "),
                        Wave(),
                        Text(" You look "),
                        Query("mood"),
                        Text("."),
                    ],
                    id: 0,
                    next: 1),
                Line("Goodbye.", speaker: 1, id: 1, next: 2),
                End(id: 2),
            ],
            ["Alice", "Bob"]);

    /// <summary>A line reading one key before its command and again after it, then the end.</summary>
    /// <remarks>
    /// <code>
    /// Smith: It was `"weapon.Attack"`. `Polish()` Now it is `"weapon.Attack"`.
    /// </code>
    /// </remarks>
    /// <returns>A context whose only line asks about the same key on each side of its command.</returns>
    public static PlayContext ALineReadingOneKeyEitherSideOfACommand() =>
        Of(
            [
                LineOf(
                    [
                        Text("It was "),
                        Query("weapon.Attack"),
                        Text(". "),
                        CustomCommand("Polish"),
                        Text(" Now it is "),
                        Query("weapon.Attack"),
                        Text("."),
                    ],
                    id: 0,
                    next: 1),
                End(id: 1),
            ],
            ["Smith"]);

    /// <summary>A menu of two options, each said as it is taken, then the end.</summary>
    /// <remarks>
    /// <code>
    /// - Go east
    /// - Go west
    /// </code>
    /// </remarks>
    /// <returns>A context that begins at the menu.</returns>
    public static PlayContext AMenu() =>
        Of(
            [
                Choice(Option("Go east", target: 1), Option("Go west", target: 2)),
                Line("Go east", id: 1, next: 3),
                Line("Go west", id: 2, next: 3),
                End(id: 3),
            ],
            [null]);

    /// <summary>A random choice among three lines, each said as it is drawn, then the end.</summary>
    /// <remarks>
    /// <code>
    /// - `50%` Guard: Halt!
    /// - `30%` Guard: Move along.
    /// - `%` Guard: Lovely day.
    /// </code>
    /// </remarks>
    /// <returns>A context that begins at the random choice.</returns>
    public static PlayContext ARandomChoice() =>
        Of(
            [
                RandomChoice(Chance(50, target: 1), Chance(30, target: 2), EvenChance(target: 3)),
                Line("Halt!", id: 1, next: 4),
                Line("Move along.", id: 2, next: 4),
                Line("Lovely day.", id: 3, next: 4),
                End(id: 4),
            ],
            ["Guard"]);

    /// <summary>A playbook that begins at a random choice whose first random option has a key weight.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// Both random options lead to the end, so the random choice is all the playbook holds to play.
    /// </remarks>
    /// <returns>A context that begins at the random choice.</returns>
    public static PlayContext ARandomChoiceWithAKeyWeightStanding() =>
        Of([ARandomChoiceWithAKeyWeight(), End(id: 1)], ["Guard"]);

    /// <summary>A loop of jumps, each leading to the next and the last back to the first.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -> node 1 -> node 2 -> ... -> node length-1 -> node 0
    /// </code>
    /// Nothing in it ever hands the host anything, so a walk with no bound never ends.
    /// </remarks>
    /// <param name="length">How many jumps the loop holds.</param>
    /// <returns>A context whose entry walks forever unless something stops it.</returns>
    public static PlayContext LoopOfJumps(int length) =>
        Of(
            [
                .. Enumerable.Range(0, length)
                    .Select(at => DivertingUnlabeled(target: (at + 1) % length, id: at)),
            ]);

    /// <summary>A chain of jumps ending at the end.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -> node 1 -> node 2 -> ... -> node jumps-1 -> node jumps, the end
    /// </code>
    /// The walk passes every node exactly once, so it is the longest walk that is not a loop.
    /// </remarks>
    /// <param name="jumps">How many jumps precede the end.</param>
    /// <returns>A context whose walk passes every node exactly once.</returns>
    public static PlayContext ChainOfJumps(int jumps) =>
        Of(
            [
                .. Enumerable.Range(0, jumps)
                    .Select(at => (Node)DivertingUnlabeled(target: at + 1, id: at)),
                End(id: jumps),
            ]);

    /// <summary>An effect, then a line, then the end.</summary>
    /// <remarks>
    /// <code>
    /// `("fade in")`
    ///
    /// Alice: Hello.
    /// </code>
    /// </remarks>
    /// <returns>A context whose playthrough waits on the host before anything is said.</returns>
    public static PlayContext AnEffectThenALine() =>
        Of(
            [
                ControlOf([DefaultCommand("fade in")], id: 0, next: 1),
                Line("Hello.", id: 1, next: 2),
                End(id: 2),
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
                Line("Away.", id: 0, next: 1, ways: [Divert("Alice.HasKey", target: 2)]),
                Line("Here.", id: 1, next: 2),
                Line("Inside.", id: 2, next: 3),
                End(id: 3),
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
                ControlOf([], id: 0, next: 1, ways: [Divert("Rainy", target: 2)]),
                Line("Onward in the sun.", id: 1, next: 2),
                Line("Inside, out of the rain.", id: 2, next: 3),
                End(id: 3),
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
    /// <returns>A context whose entry says nothing, and whose every branch says a different line.</returns>
    public static PlayContext AConditionalBlock() =>
        Of(
            [
                Branch(Gated("Alice.HasKey", target: 1), Gated("Alice.HasPick", target: 2), Else(target: 3)),
                Line("The key turns.", id: 1, next: 4),
                Line("The pick clicks.", id: 2, next: 4),
                Line("The door stays shut.", id: 3, next: 4),
                End(id: 4),
            ],
            ["Alice"]);

    /// <summary>A menu whose first option only the world can allow.</summary>
    /// <remarks>
    /// <code>
    /// - `Alice.HasKey?` Open the door
    /// - Wait here
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    public static ChoiceNode AMenuWithAConditionalOption() =>
        Choice(
            Option("Open the door", condition: "Alice.HasKey", target: 1),
            Option("Wait here", target: 2));

    /// <summary>A menu whose only option's label asks the world for words.</summary>
    /// <remarks>
    /// <code>
    /// - Call `"playerName"` over
    /// </code>
    /// </remarks>
    /// <returns>The menu.</returns>
    public static ChoiceNode AMenuWithAQueryInALabel() =>
        Choice(OptionOf([Text("Call "), Query("playerName"), Text(" over")], target: 1));

    /// <summary>A random choice whose first random option only the world can allow.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.IsAngry?` `%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice, at position 0, with both random options leading to position 1.</returns>
    public static RandomChoiceNode ARandomChoiceWithAGuardedRandomOption() =>
        RandomChoice(EvenChance(condition: "Guard.IsAngry", target: 1), EvenChance(target: 1));

    /// <summary>A random choice whose first random option has a key weight.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    /// <returns>The random choice, at position 0, with both random options leading to position 1.</returns>
    public static RandomChoiceNode ARandomChoiceWithAKeyWeight() =>
        RandomChoice(Chance("Guard.Mood", target: 1), EvenChance(target: 1));

    /// <summary>One node of every kind the playbook format defines, each the simplest of its kind.</summary>
    /// <remarks>
    /// Written out by hand, because reflection cannot choose a sensible instance of each kind; a
    /// separate test checks that the list is complete.
    /// </remarks>
    /// <returns>The nodes, each at position 0 and, unless it is the end, leading to position 1.</returns>
    public static IEnumerable<Node> OneOfEveryNodeKind() =>
    [
        Line("Hello.", id: 0, next: 1),
        End(id: 0),
        ControlOf([], id: 0, next: 1),
        ControlOf([DefaultCommand("fade in")], id: 0, next: 1),
        Choice(Option("Go east", target: 1)),
        Branch(Else(target: 1)),
        RandomChoice(EvenChance(target: 1)),
    ];

    private static CustomCommandFragment Wave() => CustomCommand("Wave");
}
