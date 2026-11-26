namespace DialogueDown.Diagnostics;

/// <summary>Checks a code a project asks to disable, for every surface that accepts one.</summary>
/// <remarks>
/// A project may disable an <c>Info</c> or <c>Warning</c> code, written in any case. Each surface
/// reports a rejected code with the same <see cref="DisabledCodeCheck.Problem"/> and adds only
/// where the code was written, such as a <c>dialogue.toml</c> location or the <c>--disable</c>
/// option.
/// </remarks>
public static class DisabledDiagnosticCodes
{
    private static readonly IReadOnlyDictionary<string, DiagnosticDescriptor> _byCode =
        DiagnosticCatalog.All.ToDictionary(descriptor => descriptor.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Checks whether a project may disable <paramref name="code"/>.</summary>
    /// <param name="code">The code as written, such as <c>dlg3002</c>.</param>
    /// <returns>The code, upper-case when the compiler reports it, and the problem if it cannot be disabled.</returns>
    public static DisabledCodeCheck Check(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!_byCode.TryGetValue(code, out var descriptor))
        {
            return new(
                code,
                $"Unknown diagnostic code '{code}'. Check the code against the error-code reference: "
                    + DiagnosticDocumentation.ReferencePage);
        }

        if (descriptor.DefaultSeverity == DiagnosticSeverity.Error)
        {
            return new(
                descriptor.Code,
                $"{descriptor.Code} is an error and cannot be disabled; only info and warning codes can. "
                    + "Fix the problem it reports instead.");
        }

        return new(descriptor.Code, Problem: null);
    }
}
