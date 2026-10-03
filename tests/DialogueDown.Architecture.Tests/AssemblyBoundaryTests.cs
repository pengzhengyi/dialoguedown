using NetArchTest.Rules;

namespace DialogueDown.Architecture.Tests;

/// <summary>
/// Assembly boundaries. The dependency direction is
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
        // The loader may use the core and Tomlyn, but no sibling satellite and no presentation or
        // host library.
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
    public void Runtime_DependsOnlyOn_ThePlaybook()
    {
        // A game embeds a playbook and a runner without the compiler, so the runner depends only
        // on the playbook.
        Types.InAssembly(Architecture.RuntimeAssembly)
            .Should()
            .OnlyHaveDependencyOn("System", Architecture.Playbook, Architecture.Runtime)
            .GetResult()
            .ShouldPass();
    }

    [Fact]
    public void Playbook_DependsOnlyOn_TheFrameworkAndGenerator()
    {
        // The playbook is the contract between a compiler and a runtime, so it depends on
        // neither. The one exception is the equality generator: it writes the records' value
        // equality at build time, and the generated code calls its small comparer assembly.
        Types.InAssembly(Architecture.PlaybookAssembly)
            .Should()
            .OnlyHaveDependencyOn("System", Architecture.Playbook, "Generator.Equals")
            .GetResult()
            .ShouldPass();
    }
}
