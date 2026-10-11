using DialogueDown.Configuration;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class StaticModeTests
{
    private const string AScene = """
        # Scene

        Alice: Hi.
        """;

    [Fact]
    public void Run_ValidDocument_WritesReportToOutputAndOpensIt()
    {
        using var tree = new TempTree();
        var script = tree.File("scene.dialogue.md", AScene);
        var output = Path.Combine(tree.Root, "report.html");
        var browser = new FakeBrowserLauncher();

        var code = StaticMode.Run(script, output, noOpen: false, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new StringWriter());

        Assert.Equal(0, code);
        Assert.True(File.Exists(output));
        Assert.StartsWith("<!doctype html", File.ReadAllText(output), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(output, Assert.Single(browser.Opened));
    }

    [Fact]
    public void Run_NoOpen_WritesReportButDoesNotOpen()
    {
        using var tree = new TempTree();
        var script = tree.File("scene.dialogue.md", AScene);
        var output = Path.Combine(tree.Root, "report.html");
        var browser = new FakeBrowserLauncher();

        var code = StaticMode.Run(script, output, noOpen: true, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new StringWriter());

        Assert.Equal(0, code);
        Assert.True(File.Exists(output));
        Assert.Empty(browser.Opened);
    }

    [Fact]
    public void Run_NoOutput_WritesATempReportAndOpensIt()
    {
        using var script = new TempScript(AScene);
        var browser = new FakeBrowserLauncher();

        var code = StaticMode.Run(script.Path, output: null, noOpen: false, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new StringWriter());

        var opened = Assert.Single(browser.Opened);
        try
        {
            Assert.Equal(0, code);
            Assert.True(File.Exists(opened));
        }
        finally
        {
            File.Delete(opened);
        }
    }

    [Fact]
    public void Run_BadDocument_ReturnsOneAndWritesError_WithoutOpening()
    {
        using var tree = new TempTree();
        var missing = Path.Combine(tree.Root, "missing.dialogue.md");
        var browser = new FakeBrowserLauncher();
        var error = new StringWriter();

        var code = StaticMode.Run(
            missing,
            output: null,
            noOpen: false,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            browser,
            error);

        Assert.Equal(1, code);
        Assert.Empty(browser.Opened);
        Assert.Contains("not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
