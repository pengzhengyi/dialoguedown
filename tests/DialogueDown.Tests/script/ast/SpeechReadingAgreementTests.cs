using DialogueDown.Emission;
using DialogueDown.Playbook.Speech;
using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

/// <summary>
/// Holds the two readings of speech to the same answer.
/// </summary>
/// <remarks>
/// The same words are read as plain text at two moments in a script's life: while it compiles, from
/// the fragments the front end built, and after it ships, from the fragments a playbook carries.
/// Those are different types in different assemblies with different lifetimes, so neither reading
/// can be written in terms of the other — yet a reader who sees a line in a report and the same
/// line in a running game must see the same words.
///
/// This is what keeps that true. A change to one reading that the other does not follow fails here,
/// rather than surfacing later as two surfaces quietly disagreeing about one line.
///
/// The fragments come from the shared samples and the second shape from the real mapping, so a
/// fragment kind added to the format arrives in this test on its own, already paired correctly.
/// </remarks>
public sealed class SpeechReadingAgreementTests
{
    [Fact]
    public void BothReadings_SayTheSameWords()
    {
        Assert.All(
            SpeakableFragments(),
            written => Assert.Equal(
                InlineText.Of([written]), SpeechText.Of([SpeechMapping.Write(written)])));
    }

    [Fact]
    public void BothReadings_NameAQueryTheSameWay()
    {
        // Worth its own test among the kinds above: it is the only one whose words are not in the
        // fragment, so it is the only one where the two readings could agree on the shape of a
        // line and still differ on what fills it.
        var written = Query("HeroName");

        Assert.Equal("{HeroName}", InlineText.Of([written]));
        Assert.Equal("{HeroName}", SpeechText.Of([SpeechMapping.Write(written)]));
    }
}
