namespace DialogueDown.Cli;

/// <summary>
/// Resolves the hosted documentation URL for a diagnostic code, so the CLI can point a reader at
/// the Error codes reference. That page is generated from the diagnostic catalog with a heading
/// per code, and the anchor is DocFX's slug of the heading: <c>### DLG1102</c> becomes
/// <c>#dlg1102</c>.
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
