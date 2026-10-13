using System.Collections.Immutable;
using DialogueDown.Configuration;

namespace DialogueDown.Compilation;

/// <summary>
/// The part of <see cref="CompilerOptions"/> a <see cref="CompilationSession"/> reads: how far a
/// compile proceeds after an error, and which diagnostic codes it marks disabled.
/// </summary>
/// <param name="Mode">How far a compile proceeds after an error.</param>
/// <param name="DisabledDiagnostics">The upper-case codes whose diagnostics are marked disabled.</param>
internal sealed record CompilationPolicy(CompilationMode Mode, IReadOnlySet<string> DisabledDiagnostics)
{
    /// <summary>The policy a compile under <paramref name="options"/> follows.</summary>
    public static CompilationPolicy For(CompilerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new CompilationPolicy(options.Mode, options.DisabledDiagnostics);
    }

    /// <summary>A policy for <paramref name="mode"/> that disables nothing.</summary>
    public static CompilationPolicy Of(CompilationMode mode) =>
        new(mode, ImmutableHashSet<string>.Empty);
}
