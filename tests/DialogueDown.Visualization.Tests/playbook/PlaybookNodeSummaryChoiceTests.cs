using static DialogueDown.TestSupport.PlaybookNodeFactory;
using static DialogueDown.TestSupport.SpeechFactory;
using static DialogueDown.Visualization.Tests.Support.PlaybookNodeSummaryAssert;

namespace DialogueDown.Visualization.Tests.Playbook;

/// <summary>
/// The summaries of the nodes holding several ways out, and of the control node, whose summary
/// depends on whether it performs anything.
/// </summary>
public sealed class PlaybookNodeSummaryChoiceTests
{
    [Fact]
    public void Of_AControl_ListsWhatTheHostPerformsInOrder()
    {
        AssertSummary(
            "ShowSprite(yuki, left); PlaySound(fire_alarm)",
            Control(CustomCommand("ShowSprite", "yuki", "left"), CustomCommand("PlaySound", "fire_alarm")));
    }

    // Brackets mean a command throughout, so one the host already knows keeps them even with no
    // arguments to hold.
    [Fact]
    public void Of_ACommandTheHostAlreadyKnows_ShowsBracketsAroundItsAction()
    {
        AssertSummary("(fade in)", Control(DefaultCommand("fade in")));
    }

    [Fact]
    public void Of_ACustomCommandWithoutArguments_StillShowsItsBrackets()
    {
        AssertSummary("Save()", Control(CustomCommand("Save")));
    }

    [Fact]
    public void Of_BothSortsOfCommand_SeparatesThemTheSameWay()
    {
        AssertSummary(
            "(crossfade); PlayMusic(battle)",
            Control(DefaultCommand("crossfade"), CustomCommand("PlayMusic", "battle")));
    }

    // A control node with no effects is what a bare scene-to-scene jump compiles to. Listing its
    // effects would say nothing at all, so it is named by the words the writer put on the divert.
    [Fact]
    public void Of_AnEffectlessControl_IsNamedByTheWordsOnItsDivert()
    {
        AssertSummary("⇒ The Mountain Road", Diverting("The Mountain Road"));
    }

    [Fact]
    public void Of_ADivertWhoseWordsArePadded_TrimsThem()
    {
        AssertSummary("⇒ The Mountain Road", Diverting("  The Mountain Road "));
    }

    [Fact]
    public void Of_AControlThatNeitherPerformsNorNamesWhereItGoes_SaysItCarriesOn()
    {
        AssertSummary("CONTINUE", DivertingUnlabeled());
    }

    [Fact]
    public void Of_AChoice_OffersTheOptionsInOrder()
    {
        AssertSummary(
            "Turn back to the crossroads || Follow the moonlight ahead",
            Choice(Option("Turn back to the crossroads"), Option("Follow the moonlight ahead")));
    }

    // An option's availability is one way out of the menu rather than the whole node, so unlike a
    // node's own condition it trails the option it governs.
    [Fact]
    public void Of_AnOptionOfferedOnlyOnACondition_MarksItAfterTheWords()
    {
        AssertSummary(
            "Brave the west road IF Alice.HasMap? || Stay put",
            Choice(Option("Brave the west road", condition: "Alice.HasMap"), Option("Stay put")));
    }

    [Fact]
    public void Of_AnOptionWithNoLabel_StandsInForTheWordsItLacks()
    {
        AssertSummary("Go left || <no label>", Choice(Option("Go left"), UnlabeledOption()));
    }

    [Fact]
    public void Of_AnOptionWhoseLabelIsPadded_TrimsIt()
    {
        AssertSummary(
            "Ask the guide for advice first || Go left",
            Choice(Option("Ask the guide for advice first "), Option("Go left")));
    }

    // A random choice's random options carry no words at all — the engine picks, so nobody is shown
    // a menu. The row reports each option's odds instead, and any condition it is drawn on.
    [Fact]
    public void Of_ARandomChoice_SaysHowManyRandomOptionsItDrawsFromAndTheirOdds()
    {
        AssertSummary(
            "DRAW 1 FROM 3: 50% || 25% || 25%",
            RandomChoice(Chance(50), Chance(25), Chance(25)));
    }

    [Fact]
    public void Of_ARandomChoiceWeightedByTheWorld_NamesTheKeyItWillAsk()
    {
        AssertSummary(
            "DRAW 1 FROM 2: {Hero.Attack} || {Dragon.Fury}",
            RandomChoice(Chance("Hero.Attack"), Chance("Dragon.Fury")));
    }

    [Fact]
    public void Of_ARandomOptionWithNoWeightOfItsOwn_SaysItSharesEvenly()
    {
        AssertSummary("DRAW 1 FROM 2: 50% || evenly", RandomChoice(Chance(50), EvenChance()));
    }

    [Fact]
    public void Of_ARandomOptionInThePoolOnlyOnACondition_MarksItAfterTheOdds()
    {
        AssertSummary(
            "DRAW 1 FROM 2: 50% IF Hero.HasMap? || 50%",
            RandomChoice(Chance(50, condition: "Hero.HasMap"), Chance(50)));
    }

    [Fact]
    public void Of_AFractionalWeight_KeepsItsDecimalAndDropsTrailingZeros()
    {
        AssertSummary("DRAW 1 FROM 2: 12.5% || 87.5%", RandomChoice(Chance(12.5), Chance(87.5)));
    }

    [Fact]
    public void Of_AConditionalControl_StillLeadsWithTheNodesOwnCondition()
    {
        AssertSummary(
            "IF Hero.IsBrave? THEN (fade out)",
            ConditionalControl("Hero.IsBrave", DefaultCommand("fade out")));
    }
}
