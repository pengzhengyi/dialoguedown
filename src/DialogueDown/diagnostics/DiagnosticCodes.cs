namespace DialogueDown.Diagnostics;

/// <summary>
/// Every diagnostic code the compiler reports, for a tool that must check or list codes, such
/// as a configuration loader validating a list of disabled codes.
/// </summary>
public static class DiagnosticCodes
{
    private static readonly IReadOnlyDictionary<string, DiagnosticCodeInfo> _byCode =
        DiagnosticCatalog.All
            .Select(Describe)
            .ToDictionary(info => info.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every code, in code order.</summary>
    public static IReadOnlyList<DiagnosticCodeInfo> All { get; } =
        [.. _byCode.Values.OrderBy(info => info.Code, StringComparer.Ordinal)];

    /// <summary>
    /// Finds a code, ignoring case, so <c>dlg3002</c> finds <c>DLG3002</c>; null when the
    /// compiler reports no such code.
    /// </summary>
    /// <param name="code">The code to look up, as written.</param>
    /// <returns>The code's description, or null.</returns>
    public static DiagnosticCodeInfo? Find(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _byCode.GetValueOrDefault(code);
    }

    /// <summary>Whether <paramref name="code"/> names a code a project may disable.</summary>
    /// <param name="code">The code to check, ignoring case.</param>
    /// <returns>True for a known <c>Info</c> or <c>Warning</c> code.</returns>
    public static bool IsOptional(string code) => Find(code)?.IsOptional ?? false;

    private static DiagnosticCodeInfo Describe(DiagnosticDescriptor descriptor) =>
        new(descriptor.Code, descriptor.Title, descriptor.Category, descriptor.DefaultSeverity);
}
