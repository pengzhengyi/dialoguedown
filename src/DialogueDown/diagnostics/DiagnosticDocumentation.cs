namespace DialogueDown.Diagnostics;

/// <summary>Where a reader looks up a diagnostic code: the hosted Error codes reference.</summary>
/// <remarks>
/// The page is generated from the diagnostic catalog with a heading per code, and a code's anchor
/// is DocFX's slug of its heading: <c>### DLG1102</c> becomes <c>#dlg1102</c>.
/// </remarks>
public static class DiagnosticDocumentation
{
    /// <summary>The hosted Error codes reference, with one section per code.</summary>
    public const string ReferencePage = "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html";

    /// <summary>The deep link to <paramref name="code"/>'s entry on the Error codes page.</summary>
    /// <param name="code">The code, such as <c>DLG1102</c>.</param>
    /// <returns>The reference page with the code's anchor, such as <c>…/error-codes.html#dlg1102</c>.</returns>
    public static string UrlFor(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        return $"{ReferencePage}#{code.ToLowerInvariant()}";
    }
}
