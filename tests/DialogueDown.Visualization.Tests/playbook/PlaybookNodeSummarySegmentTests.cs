using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Visualization.Playbook;
using static DialogueDown.Visualization.Tests.Support.PlaybookNodeFactory;

namespace DialogueDown.Visualization.Tests.Playbook;

/// <summary>
/// The labeled pieces a node's summary travels as: what each piece is, and that the characters a
/// writer typed can never change a role.
/// </summary>
public sealed class PlaybookNodeSummarySegmentTests
{
    private static readonly SpeechFragment[] _everyFragmentKind =
    [
        new TextFragment("words"),
        new StyledTextFragment(SpeechStyle.Italic, [new TextFragment("words")]),
        new LinkFragment("target", [new TextFragment("words")]),
        new ImageFragment("source.png", [new TextFragment("words")]),
        new LineBreakFragment(),
        new QueryFragment("Key"),
        new DefaultCommandFragment("fade"),
        new CustomCommandFragment("Show", ["a"]),
        new TagFragment("wise", null, false),
    ];

    [Fact]
    public void SegmentsOf_ALine_SplitsTheSpeakerFromTheWords() =>
        AssertSegments(
            Line("Take a torch.", speaker: 1),
            [("speaker", "Keeper"), ("separator", ": "), ("plain", "Take a torch.")],
            "Alice",
            "Keeper");

    [Fact]
    public void SegmentsOf_AnEnd_IsOneKeyword() =>
        AssertSegments(End(), [("keyword", "END")]);

    [Fact]
    public void SegmentsOf_AControl_KeepsEachCommandWholeAndItsSeparatorApart() =>
        AssertSegments(
            Control(DefaultCommand("fade out"), DefaultCommand("wait")),
            [
                ("boundary", ""),
                ("command", "(fade out)"),
                ("boundary", "; "),
                ("command", "(wait)"),
            ]);

    // A lone item is not a list, so no boundary is written and the summary stays the line it is.
    [Fact]
    public void SegmentsOf_AControlWithOneCommand_WritesNoBoundary() =>
        AssertSegments(Control(DefaultCommand("fade out")), [("command", "(fade out)")]);

    // The condition introduces the list rather than becoming its first item, so the boundary that
    // opens the list sits after it.
    [Fact]
    public void SegmentsOf_AConditionalControl_KeepsItsConditionBeforeTheList() =>
        AssertSegments(
            ConditionalControl("Hero.IsBrave", DefaultCommand("fade out"), DefaultCommand("wait")),
            [
                ("keyword", "IF "),
                ("query", "Hero.IsBrave?"),
                ("keyword", " THEN "),
                ("boundary", ""),
                ("command", "(fade out)"),
                ("boundary", "; "),
                ("command", "(wait)"),
            ]);

    [Fact]
    public void SegmentsOf_AControlWithNoEffects_ReadsAsTheDivertItIs() =>
        AssertSegments(
            Diverting("The Mountain Road", target: 7),
            [("separator", "⇒ "), ("target", "The Mountain Road")]);

    [Fact]
    public void SegmentsOf_AControlThatGoesNowhereNamed_SaysItCarriesOn() =>
        AssertSegments(DivertingUnlabeled(), [("keyword", "CONTINUE")]);

    [Fact]
    public void SegmentsOf_ABranch_PairsEachConditionWithTheNodeItReaches() =>
        AssertSegments(
            Branch(Arm("Alice.HasMap", target: 16), Arm(null, order: 1, target: 18)),
            [
                ("keyword", "IF "),
                ("query", "Alice.HasMap?"),
                ("keyword", " THEN "),
                ("target", "16"),
                ("keyword", " ELSE "),
                ("target", "18"),
            ]);

    [Fact]
    public void SegmentsOf_ARandomChoice_ReportsEachArmsOdds() =>
        AssertSegments(
            RandomChoice(Chance(50), Chance(50)),
            [
                ("keyword", "DRAW 1 FROM 2"),
                ("separator", ": "),
                ("boundary", ""),
                ("plain", "50%"),
                ("boundary", " || "),
                ("plain", "50%"),
            ]);

    [Fact]
    public void SegmentsOf_AGuardedOption_KeepsTheGuardAndItsKey() =>
        AssertSegments(
            Choice(Option("Brave the west road", condition: "Alice.HasMap")),
            [
                ("plain", "Brave the west road"),
                ("keyword", " IF "),
                ("query", "Alice.HasMap?"),
            ]);

    [Fact]
    public void SegmentsOf_AConditionalLine_LeadsWithItsCondition() =>
        AssertSegments(
            Line("Take a torch.", speaker: 1, condition: If("Hero.IsBrave")),
            [
                ("keyword", "IF "),
                ("query", "Hero.IsBrave?"),
                ("keyword", " THEN "),
                ("speaker", "Keeper"),
                ("separator", ": "),
                ("plain", "Take a torch."),
            ],
            "Alice",
            "Keeper");

    // The bug this change removes: every one of these was read as the table's own grammar.
    [Fact]
    public void SegmentsOf_AnOptionLabelHoldingTheSeparator_StaysOnePieceOfTheWritersWords() =>
        AssertSegments(
            Choice(Option("Go left || right"), Option("Go right")),
            [
                ("boundary", ""),
                ("plain", "Go left || right"),
                ("boundary", " || "),
                ("plain", "Go right"),
            ]);

    [Fact]
    public void SegmentsOf_ASpeakerNameHoldingAColon_StaysOneSpeakerPiece() =>
        AssertSegments(
            Line("Hello.", speaker: 0),
            [("speaker", "A: B"), ("separator", ": "), ("plain", "Hello.")],
            "A: B");

    [Fact]
    public void SegmentsOf_ACommandArgumentHoldingAComma_StaysOneCommand() =>
        AssertSegments(
            Control(CustomCommand("Show", "a, b")),
            [("command", "Show(a, b)")]);

    [Fact]
    public void SegmentsOf_ALineWithNoSpeech_MarksTheAbsence() =>
        AssertSegments(
            SilentLine(),
            [("speaker", "Alice"), ("separator", ": "), ("absent", "<no speech>")],
            "Alice");

    [Fact]
    public void SegmentsOf_AnUnlabeledOption_MarksTheAbsence() =>
        AssertSegments(Choice(UnlabeledOption()), [("absent", "<no label>")]);

    // The stand-in sits where a name would, so it stays a speaker rather than a missing value.
    [Fact]
    public void SegmentsOf_ALineByTheAnonymousSpeaker_KeepsTheStandInAsASpeaker() =>
        AssertSegments(
            Line("The room is quiet."),
            [
                ("speaker", "<anonymous>"),
                ("separator", ": "),
                ("plain", "The room is quiet."),
            ],
            [null]);

    [Fact]
    public void SegmentsOf_AQueryInSpeech_IsAQuery() =>
        AssertSegments(
            LineOf([Words("You hold "), Query("Gold"), Words(" gold.")]),
            [
                ("speaker", "<anonymous>"),
                ("separator", ": "),
                ("plain", "You hold "),
                ("query", "{Gold}"),
                ("plain", " gold."),
            ],
            [null]);

    // Only reading the fragments can tell a query from braces the writer meant literally, which is
    // the reason the projection walks them rather than scanning the flattened line.
    [Fact]
    public void SegmentsOf_BracesAWriterTyped_AreTheirWords() =>
        AssertSegments(
            LineOf([Words("Write {hello} to greet.")]),
            [
                ("speaker", "<anonymous>"),
                ("separator", ": "),
                ("plain", "Write {hello} to greet."),
            ],
            [null]);

    [Fact]
    public void SegmentsOf_AQueryInsideStyledWords_IsStillAQuery() =>
        AssertSegments(
            LineOf([Emphasized(Words("very "), Query("Gold"))]),
            [
                ("speaker", "<anonymous>"),
                ("separator", ": "),
                ("plain", "very "),
                ("query", "{Gold}"),
            ],
            [null]);

    // The cell's text is its pieces joined, so the line a reader sees is their spelling — there is
    // no second rendering that could disagree with the drawing.
    [Fact]
    public void SegmentsOf_ThePieces_JoinBackToTheLineTheTableShows()
    {
        var branch = Branch(Arm("Alice.HasMap", target: 16), Arm(null, order: 1, target: 18));

        Assert.Equal(
            "IF Alice.HasMap? THEN 16 ELSE 18",
            string.Concat(Pieces(branch).Select(piece => piece.Text)));
    }

    [Fact]
    public void SegmentsOf_ALineLongerThanTheCap_KeepsTheRolesOfThePartItKept()
    {
        var pieces = Pieces(Line(string.Join(" ", Enumerable.Repeat("word", 200))), "Alice");

        Assert.Equal("speaker", pieces[0].Role);
        Assert.Equal("Alice", pieces[0].Text);
        Assert.Equal("separator", pieces[^1].Role);
        Assert.Equal("…", pieces[^1].Text);
        Assert.True(
            string.Concat(pieces.Select(piece => piece.Text)).Length <= 201,
            "The line ran past the cap.");
    }

    [Fact]
    public void SegmentsOf_AccountsForEverySpeechFragmentKindTheFormatDeclares()
    {
        var registered = typeof(SpeechFragment)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(registration => registration.DerivedType.Name)
            .Order();

        Assert.Equal(registered, _everyFragmentKind.Select(fragment => fragment.GetType().Name).Order());
    }

    // The walk must read every fragment exactly as the format's own flattening does. Breadcrumb
    // words either side keep the line from being empty, so what the walk made of the fragment is
    // what the difference shows — and a fragment that says nothing leaves the words joined.
    [Fact]
    public void SegmentsOf_WalksEveryFragmentKindAsTheFormatDoes() =>
        Assert.All(
            _everyFragmentKind,
            fragment => Assert.Equal(
                $"before{SpeechText.Of([fragment])}after",
                string.Concat(
                    SpeechPieces([Words("before"), fragment, Words("after")])
                        .Select(piece => piece.Text))));

    // A condition is a query like any other — the boolean member of the family — so it wears the
    // query's role and the `?` the script marks it with. The sigil is written after the key
    // whatever the key holds, as the braces of a value query are.
    [Fact]
    public void SegmentsOf_AConditionKeyEndingInTheSigil_ReadsWithTwo() =>
        AssertSegments(
            Branch(Arm("Rainy?", target: 16)),
            [("keyword", "IF "), ("query", "Rainy??"), ("keyword", " THEN "), ("target", "16")]);

    // A branch's number and a jump's words are both a way to a node, so each carries the node it
    // names rather than leaving the reader to match digits against the table.
    [Fact]
    public void SegmentsOf_ABranch_NamesTheNodesItsArmsReach() =>
        AssertTargets(
            Branch(Arm("Alice.HasMap", target: 16), Arm(null, order: 1, target: 18)),
            [("16", 16), ("18", 18)]);

    [Fact]
    public void SegmentsOf_ADivert_NamesTheNodeItLeadsTo() =>
        AssertTargets(Diverting("The Mountain Road", target: 7), [("The Mountain Road", 7)]);

    // An arm's words are what the reader picks, so they name where the pick leads. The arm's guard
    // is a question about it rather than a way to it, so the guard carries nothing.
    [Fact]
    public void SegmentsOf_AMenusOptions_NameTheNodesTheyLeadTo() =>
        AssertTargets(
            Choice(
                Option("Go left", target: 3),
                Option("Go right", condition: "Alice.HasMap", target: 4)),
            [("Go left", 3), ("Go right", 4)]);

    [Fact]
    public void SegmentsOf_AnUnlabeledOption_StillNamesWhereItLeads() =>
        AssertTargets(Choice(UnlabeledOption(target: 5)), [("<no label>", 5)]);

    [Fact]
    public void SegmentsOf_ARandomChoicesOdds_NameTheNodesTheirArmsLeadTo() =>
        AssertTargets(
            RandomChoice(Chance(50, target: 6), Chance(50, target: 7)),
            [("50%", 6), ("50%", 7)]);

    // Nothing else points anywhere, so nothing else carries a node.
    [Fact]
    public void SegmentsOf_APieceThatNamesNoNode_CarriesNone() =>
        Assert.DoesNotContain(
            Pieces(Line("Take a torch.", speaker: 1), "Alice", "Keeper"),
            piece => piece.Target is not null);

    // The cut may shorten the words, but the node they name survives it.
    [Fact]
    public void SegmentsOf_APieceCutByTheCap_StillNamesItsNode()
    {
        var pieces = Pieces(Diverting(string.Join(" ", Enumerable.Repeat("word", 60)), target: 7));

        Assert.Equal("target", pieces[^2].Role);
        Assert.Equal(7, pieces[^2].Target);
        Assert.Equal("…", pieces[^1].Text);
    }

    private static void AssertTargets(Node node, (string Text, int Target)[] expected)
    {
        List<(string Text, int Target)> named = [];
        foreach (var piece in Pieces(node))
        {
            if (piece.Target is int target)
            {
                named.Add((piece.Text, target));
            }
        }

        Assert.Equal(expected, named);
    }

    private static void AssertSegments(
        Node node, (string Role, string Text)[] expected, params string?[] names) =>
        Assert.Equal(
            expected,
            Pieces(node, names).Select(piece => (piece.Role, piece.Text)));

    private static ImmutableArray<PlaybookSegmentView> Pieces(Node node, params string?[] names) =>
        PlaybookNodeSummary.SegmentsOf(node, Speakers(names));

    // A line's pieces are its speaker, its divider, and then the speech the test cares about.
    private static ImmutableArray<PlaybookSegmentView> SpeechPieces(ImmutableArray<SpeechFragment> speech) =>
        [.. Pieces(LineOf(speech), [null]).Skip(2)];
}
