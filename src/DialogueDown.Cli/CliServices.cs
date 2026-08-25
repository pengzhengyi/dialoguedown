using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Emission;
using DialogueDown.Visualization.Live;
using DialogueDown.Visualization.Live.Serving;
using Microsoft.Extensions.DependencyInjection;

namespace DialogueDown.Cli;

/// <summary>Registers the CLI's services for dependency injection.</summary>
internal static class CliServices
{
    /// <summary>Adds the CLI's collaborators to <paramref name="services"/> and returns it.</summary>
    public static IServiceCollection Register(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ProjectConfiguration>();
        services.AddSingleton<Func<CompilerOptions, IScriptCompiler>>(
            _ => options => ScriptCompilerFactory.CreateDefault(options));
        // The stream a playbook goes to when no destination is named. Injected rather than
        // reached for so a test can read what a compile emitted.
        services.AddSingleton(_ => Console.Out);
        services.AddSingleton<IPlaybookWriter>(_ => PlaybookWriterFactory.CreateDefault());
        services.AddSingleton<IErrataRenderer, ErrataRenderer>();
        services.AddSingleton<IBrowserLauncher, BrowserLauncher>();
        services.AddSingleton<IVisualizeRunner, VisualizeRunner>();
        services.AddSingleton<IServedShellRunner, ServedShellRunner>();
        return services;
    }
}
