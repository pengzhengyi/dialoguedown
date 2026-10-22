using DialogueDown.Visualization.Live.Serving;
using static DialogueDown.TestSupport.OutputAssert;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class ConsoleHostConsentTests
{
    [Theory]
    [InlineData("y")]
    [InlineData("Y")]
    [InlineData("yes")]
    [InlineData("  yes  ")]
    public void AllowHosting_Interactive_YesAllows(string answer)
    {
        var output = new StringWriter();
        var consent = new ConsoleHostConsent(interactive: true, new StringReader(answer), output);

        Assert.True(consent.AllowHosting(Request()));
        AssertReads(
            output.ToString(),
            "This document references 1 image(s) outside its folder:",
            "  /shared/a.png",
            "Allow serving files from /proj-root to render them? [y/N]");
    }

    [Theory]
    [InlineData("n")]
    [InlineData("")]
    [InlineData("nope")]
    public void AllowHosting_Interactive_AnythingButYesDeclines(string answer)
    {
        var consent = new ConsoleHostConsent(interactive: true, new StringReader(answer), new StringWriter());

        Assert.False(consent.AllowHosting(Request()));
    }

    [Fact]
    public void AllowHosting_NonInteractive_DeclinesAndSuggestsRoot()
    {
        var output = new StringWriter();
        var consent = new ConsoleHostConsent(interactive: false, new StringReader(string.Empty), output);

        Assert.False(consent.AllowHosting(Request()));
        AssertReads(
            output.ToString(),
            "This document references 1 image(s) outside its folder:",
            "  /shared/a.png",
            "Not hosting them (no interactive prompt). Re-run with --root \"/proj-root\" to allow.");
    }

    [Fact]
    public void AllowHosting_ListsEveryOutsideImage()
    {
        var output = new StringWriter();
        var consent = new ConsoleHostConsent(interactive: true, new StringReader("n"), output);

        consent.AllowHosting(new HostConsentRequest(
            "/proj/scene.dialogue.md", "/root", ["/shared/a.png", "/shared/b.png"]));

        AssertReads(
            output.ToString(),
            "This document references 2 image(s) outside its folder:",
            "  /shared/a.png",
            "  /shared/b.png",
            "Allow serving files from /root to render them? [y/N]");
    }

    [Fact]
    public void Constructor_NullInput_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new ConsoleHostConsent(interactive: true, null!, new StringWriter()));

    [Fact]
    public void Constructor_NullOutput_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new ConsoleHostConsent(interactive: true, new StringReader(string.Empty), null!));

    /// <summary>A document with one image outside its folder, and the root that would cover it.</summary>
    private static HostConsentRequest Request() =>
        new("/proj/scene.dialogue.md", "/proj-root", ["/shared/a.png"]);
}
