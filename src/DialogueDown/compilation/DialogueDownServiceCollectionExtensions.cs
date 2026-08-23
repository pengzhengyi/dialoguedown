using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Emission;
using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Markdown;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using DialogueDown.Script.Transpiler;
using DialogueDown.Script.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration extensions that add the DialogueDown compiler and its stages to a
/// dependency injection container.
/// </summary>
public static class DialogueDownServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default compiler stages and the <see cref="IScriptCompiler"/> facade
    /// as singletons, configured by <paramref name="options"/> (the unconfigured
    /// <see cref="CompilerOptions.Default"/> when null). Each stage is registered with
    /// <c>TryAdd</c>, so registering your own implementation of a stage before calling this
    /// swaps just that stage while the rest keep their defaults.
    /// </summary>
    public static IServiceCollection AddDialogueDown(
        this IServiceCollection services, CompilerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        options ??= CompilerOptions.Default;

        services.TryAddSingleton<IMarkdownParser>(
            _ => new MarkdigMarkdownParser(UnmodeledNodeHandlingPolicies.For(options.UnmodeledMarkdown)));
        services.TryAddSingleton<IScriptTranspiler>(_ => ScriptTranspilerFactory.CreateDefault());
        services.TryAddSingleton<IScriptDesugarer, ScriptDesugarer>();
        services.TryAddSingleton<IStructuralValidator>(
            _ => StructuralValidatorFactory.CreateDefault());
        services.TryAddSingleton<ISemanticAnalyzer>(_ => new SemanticAnalyzer(options.ForSemanticAnalyzer()));
        services.TryAddSingleton<IDialogueGraphBuilder>(
            _ => DialogueGraphBuilderFactory.CreateDefault());
        services.TryAddSingleton<IScriptCompiler>(provider => new ScriptCompiler(
            provider.GetRequiredService<IMarkdownParser>(),
            provider.GetRequiredService<IScriptTranspiler>(),
            provider.GetRequiredService<IScriptDesugarer>(),
            provider.GetRequiredService<IStructuralValidator>(),
            provider.GetRequiredService<ISemanticAnalyzer>(),
            provider.GetRequiredService<IDialogueGraphBuilder>(),
            options.Mode));

        // The way out of the process, and the only reason a compile leaves anything behind.
        services.TryAddSingleton<IPlaybookWriter>(_ => PlaybookWriterFactory.CreateDefault());

        return services;
    }
}
