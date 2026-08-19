using DialogueDown.Visualization.Live.Serving;
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
        Assert.Contains("Allow serving files from /proj-root", output.ToString());
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
    public void AllowHosting_NonInteractive_DeclinesAndSuggestsRenderRoot()
    {
        var output = new StringWriter();
        var consent = new ConsoleHostConsent(interactive: false, new StringReader(string.Empty), output);

        Assert.False(consent.AllowHosting(Request()));
        Assert.Contains("--render-root", output.ToString());
        Assert.DoesNotContain("[y/N]", output.ToString());
    }

    [Fact]
    public void AllowHosting_ListsEveryOutsideImage()
    {
        var output = new StringWriter();
        var consent = new ConsoleHostConsent(interactive: true, new StringReader("n"), output);

        consent.AllowHosting(new HostConsentRequest(
            "/proj/scene.dialogue.md", "/root", ["/shared/a.png", "/shared/b.png"]));

        Assert.Contains("2 image(s)", output.ToString());
        Assert.Contains("/shared/a.png", output.ToString());
        Assert.Contains("/shared/b.png", output.ToString());
    }

    [Fact]
    public void Constructor_NullInput_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new ConsoleHostConsent(interactive: true, null!, new StringWriter()));

    [Fact]
    public void Constructor_NullOutput_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new ConsoleHostConsent(interactive: true, new StringReader(string.Empty), null!));

    private static HostConsentRequest Request() =>
        new("/proj/scene.dialogue.md", "/proj-root", ["/shared/a.png"]);
}
