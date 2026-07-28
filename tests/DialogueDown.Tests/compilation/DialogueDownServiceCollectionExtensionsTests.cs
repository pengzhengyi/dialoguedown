using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler;
using DialogueDown.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static DialogueDown.Tests.Support.ConfigurationFactory;
using static DialogueDown.Tests.Support.DiagnosticsAssert;

namespace DialogueDown.Tests.Compilation;

public sealed class DialogueDownServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDialogueDown_ResolvesAWorkingCompiler()
    {
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        var result = provider.GetRequiredService<IScriptCompiler>().Compile("Alice: hi");

        Assert.Equal("Alice: hi", result.Source);
    }

    [Fact]
    public void AddDialogueDown_WithAConfiguredDefaultSpeaker_UsesItForSpeakerlessLines()
    {
        var options = new CompilerOptions { Speakers = [DefaultConfiguredSpeaker("Narrator")] };
        using var provider = new ServiceCollection().AddDialogueDown(options).BuildServiceProvider();

        var result = provider.GetRequiredService<IScriptCompiler>().Compile("Hi.");

        var speaker = result.Semantics.Speakers.Resolve(new DefaultSpeaker(SourceSpanFactory.Span()));
        Assert.Equal("Narrator", speaker.Name);
    }

    [Fact]
    public void AddDialogueDown_DefaultCompilerIncludesTheChoiceNestingRule()
    {
        var source =
            """
            - Level 1
                - Level 2
                    - Level 3
                        - Level 4
            """;
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        var result = provider.GetRequiredService<IScriptCompiler>().Compile(source);

        AssertReported(result.Diagnostics, DiagnosticCatalog.DeeplyNestedChoiceBranch);
    }

    [Fact]
    public void AddDialogueDown_RegistersTheCompilerAsASingleton()
    {
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IScriptCompiler>(),
            provider.GetRequiredService<IScriptCompiler>());
    }

    [Fact]
    public void AddDialogueDown_KeepsAPreRegisteredStage()
    {
        // A stage registered before AddDialogueDown wins, because each default uses TryAdd.
        var transpiler = Substitute.For<IScriptTranspiler>();
        transpiler.Transpile(Arg.Any<MarkdownDocument>(), Arg.Any<DiagnosticsContext>())
            .Returns(new ScriptDocument([]));
        using var provider = new ServiceCollection()
            .AddSingleton<IScriptTranspiler>(transpiler)
            .AddDialogueDown()
            .BuildServiceProvider();

        provider.GetRequiredService<IScriptCompiler>().Compile("Alice: hi");

        transpiler.Received(1)
            .Transpile(Arg.Any<MarkdownDocument>(), Arg.Is<DiagnosticsContext>(c => c!.Source == "Alice: hi"));
    }

    [Fact]
    public void AddDialogueDown_NullServices_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddDialogueDown());
}
