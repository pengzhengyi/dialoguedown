using static DialogueDown.Visualization.Tests.Support.PlaybookNodeFactory;

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
        Assert.Equal(
            "ShowSprite(yuki, left); PlaySound(fire_alarm)",
            SummaryOf(
                Control(CustomCommand("ShowSprite", "yuki", "left"), CustomCommand("PlaySound", "fire_alarm")),
                Speakers()));
    }

    // Brackets mean a command throughout, so one the host already knows keeps them even with no
    // arguments to hold.
    [Fact]
    public void Of_ACommandTheHostAlreadyKnows_WearsBracketsAroundItsAction()
    {
        Assert.Equal(
            "(fade in)", SummaryOf(Control(DefaultCommand("fade in")), Speakers()));
    }

    [Fact]
    public void Of_ACustomCommandWithoutArguments_StillShowsItsBrackets()
    {
        Assert.Equal(
            "Save()", SummaryOf(Control(CustomCommand("Save")), Speakers()));
    }

    [Fact]
    public void Of_BothSortsOfCommand_SeparatesThemTheSameWay()
    {
        Assert.Equal(
            "(crossfade); PlayMusic(battle)",
            SummaryOf(
                Control(DefaultCommand("crossfade"), CustomCommand("PlayMusic", "battle")),
                Speakers()));
    }

    // A control node with no effects is what a bare scene-to-scene jump compiles to. Listing its
    // effects would say nothing at all, so it is named by the words the writer put on the divert.
    [Fact]
    public void Of_AnEffectlessControl_IsNamedByTheWordsOnItsDivert()
    {
        Assert.Equal(
            "⇒ The Mountain Road",
            SummaryOf(Diverting("The Mountain Road"), Speakers()));
    }

    [Fact]
    public void Of_ADivertWhoseWordsArePadded_TrimsThem()
    {
        Assert.Equal(
            "⇒ The Mountain Road",
            SummaryOf(Diverting("  The Mountain Road "), Speakers()));
    }

    [Fact]
    public void Of_AControlThatNeitherPerformsNorNamesWhereItGoes_SaysItCarriesOn()
    {
        Assert.Equal("CONTINUE", SummaryOf(DivertingUnlabeled(), Speakers()));
    }

    [Fact]
    public void Of_AChoice_OffersTheOptionsInOrder()
    {
        Assert.Equal(
            "Turn back to the crossroads || Follow the moonlight ahead",
            SummaryOf(
                Choice(Option("Turn back to the crossroads"), Option("Follow the moonlight ahead")),
                Speakers()));
    }

    // An option's availability is one way out of the menu rather than the whole node, so unlike a
    // node's own condition it trails the option it governs.
    [Fact]
    public void Of_AnOptionOfferedOnlyOnACondition_MarksItAfterTheWords()
    {
        Assert.Equal(
            "Brave the west road IF Alice.HasMap || Stay put",
            SummaryOf(
                Choice(Option("Brave the west road", condition: "Alice.HasMap"), Option("Stay put")),
                Speakers()));
    }

    [Fact]
    public void Of_AnOptionWithNoLabel_StandsInForTheWordsItLacks()
    {
        Assert.Equal(
            "Go left || <no label>",
            SummaryOf(Choice(Option("Go left"), UnlabeledOption()), Speakers()));
    }

    [Fact]
    public void Of_AnOptionWhoseLabelIsPadded_TrimsIt()
    {
        Assert.Equal(
            "Ask the guide for advice first || Go left",
            SummaryOf(
                Choice(Option("Ask the guide for advice first "), Option("Go left")), Speakers()));
    }

    // A random choice's arms carry no words at all — the engine picks, so nobody is shown a menu.
    // Their odds are the only thing they hold, so the odds are what the row reports.
    [Fact]
    public void Of_ARandomChoice_SaysHowManyArmsItDrawsFromAndTheirOdds()
    {
        Assert.Equal(
            "DRAW 1 FROM 3: 50% || 25% || 25%",
            SummaryOf(
                RandomChoice(Chance(50), Chance(25), Chance(25)), Speakers()));
    }

    [Fact]
    public void Of_ARandomChoiceWeightedByTheWorld_NamesTheKeyItWillAsk()
    {
        Assert.Equal(
            "DRAW 1 FROM 2: {Hero.Attack} || {Dragon.Fury}",
            SummaryOf(
                RandomChoice(Chance("Hero.Attack"), Chance("Dragon.Fury")), Speakers()));
    }

    [Fact]
    public void Of_ARandomChoiceArmWithNoWeightOfItsOwn_SaysItSharesEvenly()
    {
        Assert.Equal(
            "DRAW 1 FROM 2: 50% || evenly",
            SummaryOf(RandomChoice(Chance(50), EvenChance()), Speakers()));
    }

    [Fact]
    public void Of_ARandomChoiceArmInThePoolOnlyOnACondition_MarksItAfterTheOdds()
    {
        Assert.Equal(
            "DRAW 1 FROM 2: 50% IF Hero.HasMap || 50%",
            SummaryOf(
                RandomChoice(Chance(50, condition: "Hero.HasMap"), Chance(50)), Speakers()));
    }

    [Fact]
    public void Of_AFractionalWeight_KeepsItsDecimalAndDropsTrailingZeros()
    {
        Assert.Equal(
            "DRAW 1 FROM 2: 12.5% || 87.5%",
            SummaryOf(RandomChoice(Chance(12.5), Chance(87.5)), Speakers()));
    }

    [Fact]
    public void Of_AConditionalControl_StillLeadsWithTheNodesOwnCondition()
    {
        Assert.Equal(
            "IF Hero.IsBrave THEN (fade out)",
            SummaryOf(
                ConditionalControl("Hero.IsBrave", DefaultCommand("fade out")), Speakers()));
    }
}
