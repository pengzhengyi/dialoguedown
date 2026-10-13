using System.Collections.Immutable;
using DialogueDown.Common;
using DialogueDown.Diagnostics;
using Generator.Equals;

namespace DialogueDown.Configuration;

/// <summary>
/// Deeply immutable options that configure a single compile — the public value a consumer builds,
/// in code or by loading a <c>dialogue.toml</c>, and hands to a composition root. The root
/// separates it into each stage's options view. Start from <see cref="Default"/> and adjust it with
/// a <c>with</c> expression.
/// </summary>
[Equatable]
public sealed partial record CompilerOptions
{
    [IgnoreEquality]
    private ImmutableArray<ConfiguredSpeaker> _speakers = ImmutableArray<ConfiguredSpeaker>.Empty;

    [IgnoreEquality]
    private ImmutableDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> _unmodeledMarkdown =
        ImmutableDictionary<UnmodeledNodeKind, UnmodeledNodeHandling>.Empty;

    [IgnoreEquality]
    private ImmutableHashSet<string> _disabledDiagnostics = ImmutableHashSet<string>.Empty;

    /// <summary>Creates the unconfigured options: every knob at its built-in default.</summary>
    public CompilerOptions()
    {
    }

    /// <summary>Creates immutable snapshots of every configured collection.</summary>
    public CompilerOptions(
        CompilationMode mode,
        IEnumerable<ConfiguredSpeaker> speakers,
        IEnumerable<KeyValuePair<UnmodeledNodeKind, UnmodeledNodeHandling>> unmodeledMarkdown)
    {
        ArgumentNullException.ThrowIfNull(speakers);
        ArgumentNullException.ThrowIfNull(unmodeledMarkdown);

        Mode = mode;
        Speakers = speakers.ToImmutableArray();
        UnmodeledMarkdown = unmodeledMarkdown.ToImmutableDictionary();
    }

    /// <summary>
    /// Creates immutable snapshots of every configured collection, including the
    /// <paramref name="disabledDiagnostics"/> codes.
    /// </summary>
    public CompilerOptions(
        CompilationMode mode,
        IEnumerable<ConfiguredSpeaker> speakers,
        IEnumerable<KeyValuePair<UnmodeledNodeKind, UnmodeledNodeHandling>> unmodeledMarkdown,
        IEnumerable<string> disabledDiagnostics)
        : this(mode, speakers, unmodeledMarkdown)
    {
        ArgumentNullException.ThrowIfNull(disabledDiagnostics);
        DisabledDiagnostics = disabledDiagnostics.ToImmutableHashSet();
    }

    /// <summary>
    /// Speakers supplied by configuration — an immutable registry seeded alongside a script's own
    /// speakers. At most one may be the default speaker, and a script's own <c>##default</c> takes
    /// precedence over it.
    /// </summary>
    [OrderedEquality]
    public ImmutableArray<ConfiguredSpeaker> Speakers
    {
        get => _speakers;
        init => _speakers = value.AssertInitialized(nameof(Speakers));
    }

    /// <summary>How far a compile proceeds after an error; the default is
    /// <see cref="CompilationMode.StageBoundary"/>.</summary>
    public CompilationMode Mode { get; init; } = CompilationMode.StageBoundary;

    /// <summary>
    /// How the front-end handles each kind of unmodeled Markdown construct, overriding the
    /// built-in defaults. Only the kinds named here differ; every other kind keeps its default,
    /// so an empty map (the default) is the built-in behavior.
    /// </summary>
    [UnorderedEquality]
    public ImmutableDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> UnmodeledMarkdown
    {
        get => _unmodeledMarkdown;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _unmodeledMarkdown = value;
        }
    }

    /// <summary>
    /// The diagnostic codes a project disabled, stored upper-case: a disabled diagnostic is still
    /// reported, marked <see cref="LocatedDiagnostic.IsDisabled"/>. Empty by default. Only an
    /// <c>Info</c> or <c>Warning</c> code may be disabled; setting an unknown or <c>Error</c> code
    /// throws <see cref="ArgumentException"/>.
    /// </summary>
    [UnorderedEquality]
    public ImmutableHashSet<string> DisabledDiagnostics
    {
        get => _disabledDiagnostics;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _disabledDiagnostics = [.. value.Select(NormalizeDisabledCode)];
        }
    }

    /// <summary>The unconfigured options: every knob at its built-in default.</summary>
    public static CompilerOptions Default { get; } = new();

    /// <summary>The part of these options the semantic analysis stage reads.</summary>
    internal ISemanticAnalyzerOptions ForSemanticAnalyzer() => new SemanticAnalyzerOptions(Speakers);

    private static string NormalizeDisabledCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var info = DiagnosticCodes.Find(code)
            ?? throw new ArgumentException(
                $"Cannot disable '{code}': no diagnostic has that code.", nameof(code));
        if (!info.IsOptional)
        {
            throw new ArgumentException(
                $"Cannot disable {info.Code}: it is an error, and only info and warning codes can be "
                + "disabled.",
                nameof(code));
        }

        return info.Code;
    }
}
