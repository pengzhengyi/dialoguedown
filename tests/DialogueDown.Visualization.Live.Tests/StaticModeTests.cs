using DialogueDown.Configuration;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class StaticModeTests
{
    [Fact]
    public void Run_ValidDocument_WritesReportToOutputAndOpensIt()
    {
        using var script = new TempScript("# Scene\n\nAlice: Hi.");
        var browser = new FakeBrowserLauncher();
        var output = TempHtmlPath();

        try
        {
            var code = StaticMode.Run(script.Path, output, noOpen: false, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new StringWriter());

            Assert.Equal(0, code);
            Assert.True(File.Exists(output));
            Assert.StartsWith(
                "<!doctype html",
                File.ReadAllText(output),
                StringComparison.OrdinalIgnoreCase);
            var opened = Assert.Single(browser.Opened);
            Assert.Equal(output, opened);
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Fact]
    public void Run_NoOpen_WritesReportButDoesNotOpen()
    {
        using var script = new TempScript("# Scene\n\nAlice: Hi.");
        var browser = new FakeBrowserLauncher();
        var output = TempHtmlPath();

        try
        {
            var code = StaticMode.Run(script.Path, output, noOpen: true, AppliedConfiguration.WithoutFile(CompilerOptions.Default), browser, new StringWriter());

            Assert.Equal(0, code);
            Assert.True(File.Exists(output));
            Assert.Empty(browser.Opened);
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Fact]
    public void Run_NoOutput_WritesATempReportAndOpensIt()
    {
        using var script = new TempScript("# Scene\n\nAlice: Hi.");
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
        var browser = new FakeBrowserLauncher();
        var error = new StringWriter();

        var code = StaticMode.Run(
            "/tmp/missing.dialogue.md",
            output: null,
            noOpen: false,
            AppliedConfiguration.WithoutFile(CompilerOptions.Default),
            browser,
            error);

        Assert.Equal(1, code);
        Assert.Empty(browser.Opened);
        Assert.Contains("not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static string TempHtmlPath() =>
        Path.Combine(Path.GetTempPath(), $"dd-out-{Guid.NewGuid():N}.html");
}
