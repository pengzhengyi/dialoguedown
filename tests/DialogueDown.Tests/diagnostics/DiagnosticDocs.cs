using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

internal static class DiagnosticDocs
{
    // Codes documented without an example because no default compile can produce them yet. When
    // their producer or registration lands, give the code an example and remove it here;
    // DiagnosticDocsTests enforces that this list stays honest.
    public static IReadOnlySet<string> WithoutExampleYet { get; } =
        new HashSet<string>
        {
            DiagnosticCatalog.DisallowedLabelElement.Code,
        };

    public static IReadOnlyList<DiagnosticDoc> All { get; } =
    [
        new(
            DiagnosticCatalog.UnreachableContentAfterJump,
            "A jump does not return, so text or a second jump after it on the same line never plays. "
            + "Put each jump on its own line, separated by a blank line, so nothing trails it.",
            new(
                """
                # Crossroads
                => [Market](#market) or => [Home](#home)

                # Market
                Merchant: Wares!

                # Home
                Alice: Cozy.
                """,
                """
                # Crossroads
                => [Market](#market)

                => [Home](#home)

                # Market
                Merchant: Wares!

                # Home
                Alice: Cozy.
                """,
                ["or => [Home](#home)"],
                ["=> [Home](#home)"])),
        new(
            DiagnosticCatalog.TagsWithoutSpeaker,
            "A line that begins with tags but no name has nothing to attach the tags to. Start the "
            + "line with a speaker's name, or use an `@id` to add tags to a speaker already declared.",
            new(
                """
                # Scene
                #excited: We made it!
                """,
                """
                # Scene
                Alice #excited: We made it!
                """,
                ["#excited"],
                ["Alice "])),
        new(
            DiagnosticCatalog.NotAGameCall,
            "A code span calls into the game. Its contents must be a query that reads a value, a "
            + "default command, or a named command — plain words are not a call.",
            new(
                """
                # Scene
                Alice: The sky turns `just some words`.
                """,
                """
                # Scene
                Alice: The sky turns `"World.Weather"`.
                """,
                ["just some words"],
                [""" "World.Weather" """.Trim()])),
        new(
            DiagnosticCatalog.DisallowedLabelElement,
            "A jump or link label is plain, styled text only. Functional elements — code spans, "
            + "images, nested links, or line breaks — are not allowed inside a label or an image's "
            + "alt text."),
        new(
            DiagnosticCatalog.MissingChoiceWeight,
            "In a random choice — a list where at least one option leads with a weight — every "
            + "option must carry a weight so the engine can pick fairly. Give the option a "
            + "percentage like `50%`, or `%` to share the remaining percentage equally.",
            new(
                """
                # Coin
                The coin spins.

                - `50%` Heads.
                - Tails.
                """,
                """
                # Coin
                The coin spins.

                - `50%` Heads.
                - `50%` Tails.
                """,
                ["- Tails."],
                ["`50%` Tails."])),
        new(
            DiagnosticCatalog.InvalidChoiceWeight,
            "A choice weight is a percentage code span: a non-negative number like `50%`, a bare "
            + "`%` to take an equal share of the remaining percentage, or a game-state key like "
            + "`Luck%` the runtime computes into a weight. A negative number is not a valid weight.",
            new(
                """
                # Coin
                The coin spins.

                - `-10%` Heads.
                - `%` Tails.
                """,
                """
                # Coin
                The coin spins.

                - `10%` Heads.
                - `%` Tails.
                """,
                ["`-10%`"],
                ["`10%`"])),
        new(
            DiagnosticCatalog.OrphanCondition,
            "A condition guards the jump it precedes, the line it fronts, the choice option it "
            + "leads, or the control branch it opens. A `\"key\"?` code span anywhere else has "
            + "nothing to guard. Move it to one of those positions, or remove the `?` to write a "
            + "plain query.",
            new(
                """
                # Moor
                Guide: `"Rainy"?` The moor is bleak.
                """,
                """
                # Moor
                `"Rainy"?` Guide: The moor is bleak.
                """,
                ["`\"Rainy\"?`"],
                ["`\"Rainy\"?`"])),
        new(
            DiagnosticCatalog.StyledSpeakerPrefix,
            "A line that begins with a styled name followed by a colon — like `*Alice*:` — looks "
            + "like a speaker prefix, but the styling stops it from being recognized, so the line "
            + "has no speaker. Remove the styling from the name.",
            new(
                """
                *Alice*: Hello there.
                """,
                """
                Alice: Hello there.
                """,
                ["*Alice*:"],
                ["Alice:"])),
        new(
            DiagnosticCatalog.SeveredControlBranch,
            "Every branch of a block conditional belongs to one connected blockquote. An `elseif` "
            + "or `else` that starts another blockquote has no connected `if`; continue the original "
            + "blockquote instead.",
            new(
                """
                > `elseif` `Known?`
                >
                > Alice: Welcome back.
                """,
                """
                > `if` `Known?`
                >
                > Alice: Welcome back.
                """,
                ["`elseif`"],
                ["`if`"])),
        new(
            DiagnosticCatalog.MalformedControlBranchOrder,
            "A block conditional has one `if`, then any `elseif` branches, then at most one `else`. "
            + "Move a conditional branch before the fallback instead of adding another `else` afterward.",
            new(
                """
                > `if` `Rich?`
                >
                > Alice: Welcome upstairs.
                >
                > `else`
                >
                > Alice: Try downstairs.
                >
                > `else`
                >
                > Alice: Welcome back.
                """,
                """
                > `if` `Rich?`
                >
                > Alice: Welcome upstairs.
                >
                > `elseif` `Known?`
                >
                > Alice: Welcome back.
                >
                > `else`
                >
                > Alice: Try downstairs.
                """,
                ["`else`\n>\n> Alice: Welcome back."],
                ["`elseif` `Known?`\n>\n> Alice: Welcome back."])),
        new(
            DiagnosticCatalog.ControlMarkerNotAlone,
            "A branch marker is its own paragraph. Keep the blockquote connected, but add a quoted "
            + "blank line (`>`) before the branch body so Markdown does not fuse them together.",
            new(
                """
                > `if` `Rich?`
                > Alice: Welcome upstairs.
                """,
                """
                > `if` `Rich?`
                >
                > Alice: Welcome upstairs.
                """,
                ["`Rich?`\n> Alice"],
                ["`Rich?`\n>\n> Alice"])),
        new(
            DiagnosticCatalog.MissingControlBranchCondition,
            "An `if` or `elseif` marker needs its condition in a second code span. Add a condition such "
            + "as `Rich?`; only `else` is unconditional.",
            new(
                """
                > `if`
                >
                > Alice: Welcome upstairs.
                """,
                """
                > `if` `Rich?`
                >
                > Alice: Welcome upstairs.
                """,
                ["> `if`\n>"],
                ["> `if` `Rich?`"])),
        new(
            DiagnosticCatalog.UnexpectedElseCondition,
            "An `else` is the unconditional fallback, so it cannot carry a condition. Remove the condition, "
            + "or write `elseif` when the branch should be conditional.",
            new(
                """
                > `if` `Rich?`
                >
                > Alice: Welcome upstairs.
                >
                > `else` `Known?`
                >
                > Alice: Welcome back.
                """,
                """
                > `if` `Rich?`
                >
                > Alice: Welcome upstairs.
                >
                > `else`
                >
                > Alice: Welcome back.
                """,
                ["`else` `Known?`"],
                ["`else`"])),
        new(
            DiagnosticCatalog.DanglingJumpArrow,
            "`=>` is the jump sigil: it becomes a jump only when a Markdown link follows it. With "
            + "no link there is nothing to jump to, so the arrow is read literally — it stays on "
            + "the page as the two characters and the script simply continues to the next line. "
            + "That is fine when you meant to type an arrow; when you meant to jump, give it a "
            + "target.",
            new(
                """
                # Crossroads
                Alice: Which way?

                => The market

                # The market
                Merchant: Wares!
                """,
                """
                # Crossroads
                Alice: Which way?

                => [The market](#the-market)

                # The market
                Merchant: Wares!
                """,
                ["=> The market"],
                ["=> [The market](#the-market)"])),
        new(
            DiagnosticCatalog.IgnoredUnmodeledMarkdown,
            "DialogueDown models the Markdown a dialogue needs; everything else is an authoring "
            + "aid. A code block, a table, or a divider is left out of the script rather than "
            + "spoken, which is usually the point — a diagram or a note belongs beside the "
            + "dialogue, not in it. If that is what you meant, keep it: this is a note, not a "
            + "fault, and nothing about the compile changes. It exists so the omission is never "
            + "a surprise. If the construct was meant to shape the dialogue, write it in "
            + "DialogueDown's own terms — a scene break is a heading. If it arrived by accident, "
            + "remove it.",
            new(
                """
                # Chapter One

                Alice: We should go.

                ---

                Alice: The road was long.
                """,
                """
                # Chapter One

                Alice: We should go.

                # On The Road

                Alice: The road was long.
                """,
                ["---"],
                ["# On The Road"],
                "if it was meant to break the scene",
                new(
                    "if it arrived by accident",
                    """
                    # Chapter One

                    Alice: We should go.

                    Alice: The road was long.
                    """,
                    ["Alice: The road was long."]))),
        new(
            DiagnosticCatalog.DuplicateAnchor,
            "Each scene heading becomes a jump target — an anchor slugged from its text. Two headings "
            + "with the same text produce the same anchor, so a jump to it is ambiguous.",
            new(
                """
                # Chapter
                Alice: Hello.

                # Chapter
                Bob: Goodbye.
                """,
                """
                # Chapter One
                Alice: Hello.

                # Chapter Two
                Bob: Goodbye.
                """,
                [],
                [" One", " Two"])),
        new(
            DiagnosticCatalog.HeadingWithoutAnchor,
            "A heading becomes a jump target only if it has letters or numbers to slug into an "
            + "anchor. A heading of punctuation alone can never be jumped to.",
            new(
                """
                # ...
                Alice: Hello.
                """,
                """
                # Prologue
                Alice: Hello.
                """,
                ["..."],
                ["Prologue"])),
        new(
            DiagnosticCatalog.SpeakerNameIdConflict,
            "A name and an `@id` were each used on their own for different speakers, so binding them "
            + "together now is ambiguous. Declare the pairing once, up front, before either is used "
            + "alone.",
            new(
                """
                Alice: Hello.

                @A: Over here.

                Alice @A: It is me.
                """,
                """
                Alice @A: It is me.

                Alice: Hello.

                @A: Over here.
                """,
                ["Alice @A"],
                ["Alice @A"])),
        new(
            DiagnosticCatalog.IdBoundToAnotherName,
            "An `@id` is a stable handle for one speaker, so it cannot name two. Give the second "
            + "speaker its own id.",
            new(
                """
                Alice @A: Hi.

                Bob @A: Hello.
                """,
                """
                Alice @A: Hi.

                Bob @B: Hello.
                """,
                ["Bob @A"],
                ["@B"])),
        new(
            DiagnosticCatalog.NameBoundToAnotherId,
            "A speaker has one stable `@id`. Binding the same name to a second id is a conflict — "
            + "give the speaker a single id everywhere.",
            new(
                """
                Alice @A: Hi.

                Alice @B: Hello again.
                """,
                """
                Alice @A: Hi.

                Alice @A: Hello again.
                """,
                ["Alice @B"],
                [])),
        new(
            DiagnosticCatalog.MultipleDefaultSpeakers,
            "The default speaker covers lines that name no one, so a script can have only one. Mark "
            + "just a single speaker `##default`.",
            new(
                """
                Alice ##default: Hi.

                Bob ##default: Hello.
                """,
                """
                Alice ##default: Hi.

                Bob: Hello.
                """,
                ["Alice ##default", "Bob ##default"],
                ["Alice ##default"])),
        new(
            DiagnosticCatalog.UnnamedSpeakerId,
            "A stable `@id` must belong to a named speaker. This id is referenced but never declared "
            + "with a name — declare it once with `Name @id:`.",
            new(
                """
                # Scene
                @ghost: Who goes there?
                """,
                """
                # Scene
                Ghost @ghost: Who goes there?
                """,
                ["@ghost"],
                ["Ghost "])),
        new(
            DiagnosticCatalog.UnknownReservedTag,
            "A `##name` tag is a reserved, built-in tag, and `##default` is the only one DialogueDown "
            + "knows. For your own metadata use a custom tag with a single `#`.",
            new(
                """
                # Scene
                Alice ##hero: To the rescue!
                """,
                """
                # Scene
                Alice #hero: To the rescue!
                """,
                ["##hero"],
                ["#hero"])),
        new(
            DiagnosticCatalog.MissingScene,
            "A jump must point at a scene that exists in the file. This jump's anchor matches no "
            + "heading — check the spelling, or add the scene it should reach.",
            new(
                """
                # Start
                Alice: Onward!

                => [Continue](#the-end)
                """,
                """
                # Start
                Alice: Onward!

                => [Continue](#the-end)

                # The End
                Alice: We made it.
                """,
                ["#the-end"],
                ["# The End"])),
        new(
            DiagnosticCatalog.ZeroChoiceWeightTotal,
            "A random choice picks one option by weight. When every weight is 0 there is nothing "
            + "to pick from — the odds are undefined. Give at least one option a positive weight.",
            new(
                """
                # Coin
                The coin spins.

                - `0%` Heads.
                - `0%` Tails.
                """,
                """
                # Coin
                The coin spins.

                - `50%` Heads.
                - `50%` Tails.
                """,
                ["`0%`"],
                ["`50%`"])),
        new(
            DiagnosticCatalog.OptionWithNothingToShow,
            "A menu shows each option by the words written in it — the line it speaks, or the "
            + "text of the jump it makes. An option with neither leaves the player a blank line "
            + "to pick. The compiler will not read words off the scene the option leads to, "
            + "because those belong to whoever wrote them, so the option stays as blank as it "
            + "was written.",
            new(
                """
                Alice: Which way?

                - `("fade out")`
                - Alice: Stay here.
                """,
                """
                Alice: Which way?

                - Slip away quietly `("fade out")`
                - Alice: Stay here.
                """,
                ["- `(\"fade out\")`"],
                ["- Slip away quietly `(\"fade out\")`"])),
        new(
            DiagnosticCatalog.ExternalJumpNotResolved,
            "A jump reaches a scene in the script it is written in. Reaching one in another script "
            + "is not built yet, so a target naming a file or a URL resolves to nothing and the "
            + "line simply reads on. Keep the destination in this script until cross-file jumps "
            + "land.",
            new(
                """
                Alice: To the vault.

                => [The vault](chapter-02.md#the-vault)
                """,
                """
                Alice: To the vault.

                => [The vault](#the-vault)

                # The vault
                """,
                ["chapter-02.md#the-vault"],
                ["#the-vault"])),
        new(
            DiagnosticCatalog.SceneHeadingInsideBranch,
            "Scene headings define document-level jump targets. A heading inside a control branch "
            + "or choice option would not create a scene, so move it outside the branch and jump to "
            + "it when that path should enter the scene.",
            new(
                """
                > `if` `Rich?`
                >
                > # Upstairs
                >
                > Alice: Welcome.
                """,
                """
                # Upstairs

                > `if` `Rich?`
                >
                > Alice: Welcome.
                """,
                ["> # Upstairs"],
                ["# Upstairs"])),
        new(
            DiagnosticCatalog.DeeplyNestedChoiceBranch,
            "Nested choices remain valid, but a fourth level becomes difficult to scan and "
            + "maintain. Consider moving that branch into a new scene and jumping to it instead.",
            new(
                """
                # Conversation

                - Level 1
                    - Level 2
                        - Level 3
                            - Level 4
                                Alice: This branch is difficult to scan.
                """,
                """
                # Conversation

                - Level 1
                    - Level 2
                        => [Continue](#deeper-branch)

                # Deeper branch

                - Level 3
                    - Level 4
                        Alice: This branch is easier to scan.
                """,
                ["- Level 4"],
                ["=> [Continue](#deeper-branch)", "# Deeper branch"])),
        new(
            DiagnosticCatalog.ChoiceWeightsNotOneHundred,
            "A random choice's weights are relative — they are normalized by their sum — so any "
            + "positive total works. When they do not add up to 100 the intended odds are harder "
            + "to read; adjust them to total 100% (or use `%` to share the rest) to state the odds "
            + "directly.",
            new(
                """
                # Coin
                The coin spins.

                - `50%` Heads.
                - `30%` Tails.
                """,
                """
                # Coin
                The coin spins.

                - `50%` Heads.
                - `50%` Tails.
                """,
                ["`30%`"],
                ["`50%`"])),
        new(
            DiagnosticCatalog.SingleOptionRandomChoice,
            "A random choice with only one option always selects it — the weight has no effect and "
            + "the list is not really random. This usually means a plain line was given a weight, "
            + "or the other options are missing.",
            new(
                """
                # Coin
                The coin spins.

                - `50%` It always lands heads.
                """,
                """
                # Coin
                The coin spins.

                - `50%` Heads.
                - `50%` Tails.
                """,
                ["`50%` It always lands heads."],
                ["`50%` Tails."])),
    ];

    public static IReadOnlyDictionary<string, DiagnosticDoc> ByCode { get; } =
        All.ToDictionary(doc => doc.Descriptor.Code);
}
