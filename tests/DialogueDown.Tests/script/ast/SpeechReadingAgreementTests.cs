using DialogueDown.Emission;
using DialogueDown.Playbook.Speech;
using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

/// <summary>
/// Holds the two readings of speech to the same answer.
/// </summary>
/// <remarks>
/// Speech is read as plain text at two points: while a script compiles, from the fragments the
/// front end built, and after it ships, from the fragments a playbook carries. The two are
/// different types in different assemblies, so this checks they give the same words.
/// <para>
/// The fragments come from the shared samples and their playbook form from the real mapping, so a
/// fragment kind added to the samples is checked here without a change to this test.
/// </para>
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
        // A query is the one kind whose words are not in the fragment; both readings show its key
        // in braces.
        var written = Query("HeroName");

        Assert.Equal("{HeroName}", InlineText.Of([written]));
        Assert.Equal("{HeroName}", SpeechText.Of([SpeechMapping.Write(written)]));
    }
}
