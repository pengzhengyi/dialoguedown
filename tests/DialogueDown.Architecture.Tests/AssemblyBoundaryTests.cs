using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Group A — assembly boundaries. The dependency direction is
/// <c>Cli -> Visualization.Live -> Visualization -> Core</c>, and it must never
/// reverse: lower layers stay unaware of the layers built on top of them. The
/// configuration loader is a parallel satellite that depends only on the core (and
/// Tomlyn), and the core stays unaware of it.
/// </summary>
public sealed class AssemblyBoundaryTests
{
    [Fact]
    public void Core_DoesNotDependOn_CliVisualizationOrLive()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Cli,
                Architecture.Visualization,
                Architecture.VisualizationLive)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Core_DoesNotDependOn_ConfigurationLoader()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Architecture.ConfigurationLoader)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void ConfigurationLoader_DependsOnlyOn_CoreAndToml()
    {
        // The loader may reach for the core and Tomlyn; it must stay unaware of every sibling
        // satellite and of any presentation or host library.
        Types.InAssembly(Architecture.ConfigurationLoaderAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Cli,
                Architecture.Visualization,
                Architecture.VisualizationLive,
                Architecture.SpectreConsole,
                Architecture.Godot,
                Architecture.SystemConsole)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Core_DoesNotDependOn_PresentationOrHostLibraries()
    {
        Types.InAssembly(Architecture.CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.SpectreConsole,
                Architecture.Godot,
                Architecture.SystemConsole)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Visualization_DoesNotDependOn_CliOrLive()
    {
        Types.InAssembly(Architecture.VisualizationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                Architecture.Cli,
                Architecture.VisualizationLive)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void VisualizationLive_DoesNotDependOn_Cli()
    {
        Types.InAssembly(Architecture.VisualizationLiveAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Architecture.Cli)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Playbook_DependsOnNothing()
    {
        // The playbook is the contract between a compiler and a runtime, so it must
        // belong to neither. A game embeds it alongside a runner; if it reached for
        // the compiler, every shipped game would carry the Markdown parser, the TOML
        // reader, and the diagnostics engine with it.
        Types.InAssembly(Architecture.PlaybookAssembly)
            .Should()
            .OnlyHaveDependencyOn("System", Architecture.Playbook)
            .GetResult()
            .ShouldPass();
    }
}
