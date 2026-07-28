namespace DialogueDown.Cli;

/// <summary>
/// Resolves the hosted documentation URL for a diagnostic code, so the CLI can point a reader at
/// the Error codes reference. The anchor mirrors the DocFX-slugged heading on that page — a
/// <c>### DLG1102</c> heading becomes <c>#dlg1102</c> — which the docs generator keeps in lockstep
/// with the diagnostic catalog.
/// </summary>
internal static class DiagnosticDocumentation
{
    private const string ErrorCodesPage =
        "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html";

    /// <summary>The deep link to <paramref name="code"/>'s entry on the Error codes page.</summary>
    public static string UrlFor(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        return $"{ErrorCodesPage}#{code.ToLowerInvariant()}";
    }
}
