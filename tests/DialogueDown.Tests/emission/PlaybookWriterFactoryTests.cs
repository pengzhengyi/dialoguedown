using DialogueDown.Emission;
using DialogueDown.Playbook.Checking;
using DialogueDown.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DialogueDown.Tests.Emission;

public sealed class PlaybookWriterFactoryTests
{
    private const string Script = "chapter-01.dialogue.md";

    [Fact]
    public void CreateDefault_AWriter_WritesAPlaybookThisBuildCanRead()
    {
        var playbook = PlaybookWriterFactory.CreateDefault().Write(Pipeline.Compiled("Alice: Hello."), Script);

        PlaybookCheckerFactory.CreateDefault().Check(playbook);
    }

    [Fact]
    public void AddDialogueDown_ResolvingAWriter_GivesTheSameOneTheFactoryDoes()
    {
        // Both composition roots exist so a caller can pick, not so they can differ.
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        var resolved = provider.GetRequiredService<IPlaybookWriter>();

        Assert.Equal(
            PlaybookWriterFactory.CreateDefault().GetType(),
            resolved.GetType());
    }

    [Fact]
    public void AddDialogueDown_AWriterOfYourOwn_IsLeftAlone()
    {
        // Registered with TryAdd like every other stage, so swapping one swaps just that one.
        var mine = Substitute.For<IPlaybookWriter>();
        using var provider = new ServiceCollection()
            .AddSingleton<IPlaybookWriter>(mine)
            .AddDialogueDown()
            .BuildServiceProvider();

        Assert.Same(mine, provider.GetRequiredService<IPlaybookWriter>());
    }
}
