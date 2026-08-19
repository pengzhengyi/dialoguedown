using DialogueDown.Cli.Infrastructure;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Visualization.Live;
using DialogueDown.Visualization.Live.Launching;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli.Testing;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>
/// Builds a <see cref="CommandAppTester"/> wired exactly like production
/// (<see cref="CliServices"/> + <see cref="CliConfigurator"/>), optionally
/// substituting the script compiler, the compiler factory, or the visualize runner so
/// command behavior can be tested in isolation.
/// </summary>
internal static class CliTester
{
    /// <summary>
    /// Creates a tester. A given <paramref name="compiler"/> replaces the default compiler
    /// (wrapped as a factory that ignores the options); a <paramref name="compilerFactory"/>
    /// replaces the factory itself (to assert the resolved options); a
    /// <paramref name="runner"/> or <paramref name="launcher"/> replaces its registration
    /// (last wins).
    /// </summary>
    public static CommandAppTester Create(
        IScriptCompiler? compiler = null,
        IVisualizeRunner? runner = null,
        ILauncherRunner? launcher = null,
        Func<CompilerOptions, IScriptCompiler>? compilerFactory = null)
    {
        var services = new ServiceCollection();
        CliServices.Register(services);
        if (compiler is not null)
        {
            services.AddSingleton<Func<CompilerOptions, IScriptCompiler>>(_ => _ => compiler);
        }

        if (compilerFactory is not null)
        {
            services.AddSingleton(compilerFactory);
        }

        if (runner is not null)
        {
            services.AddSingleton(runner);
        }

        if (launcher is not null)
        {
            services.AddSingleton(launcher);
        }

        var tester = new CommandAppTester(new TypeRegistrar(services));
        tester.Configure(CliConfigurator.Configure);
        return tester;
    }
}
