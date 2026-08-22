using DialogueDown.Emission;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Emission;

public sealed class SpeakerMappingTests
{
    [Fact]
    public void Write_ASpeaker_KeepsWhatTheScriptSaidAboutThem()
    {
        var alice = SpeakerSymbol.ForId("A");
        alice.GiveName("Alice");
        alice.MarkDefault();

        var written = SpeakerMapping.Write(alice);

        Assert.Equal("A", written.Id);
        Assert.Equal("Alice", written.Name);
        Assert.True(written.Default);
    }

    [Fact]
    public void Write_ASpeakerTheWriterNeverNamed_CarriesNeitherNameNorId()
    {
        // A script that declares nobody still says lines; they belong to a speaker with no name.
        var written = SpeakerMapping.Write(SpeakerSymbol.Anonymous());

        Assert.Null(written.Name);
        Assert.Null(written.Id);
        Assert.False(written.Default);
    }

    [Fact]
    public void Write_ASpeakersTags_KeepsThemInOrderAndSaysWhichAreReserved()
    {
        var alice = SpeakerSymbol.ForName("Alice");
        alice.MergeTag(CustomTag("mood", "wry"));
        alice.MergeTag(ReservedTag("aside"));

        var written = SpeakerMapping.Write(alice);

        Assert.Equal(["mood", "aside"], written.Tags.Select(tag => tag.Name));
        Assert.Equal(["wry", null], written.Tags.Select(tag => tag.Value));
        Assert.Equal([false, true], written.Tags.Select(tag => tag.Reserved));
    }

    [Fact]
    public void Write_ASpeakerWithNothingSaidAboutThem_CarriesNoTags()
    {
        Assert.Empty(SpeakerMapping.Write(SpeakerSymbol.ForName("Alice")).Tags);
    }

    [Fact]
    public void Write_NobodyAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SpeakerMapping.Write(null!));
    }
}
