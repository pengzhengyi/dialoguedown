using System.Text.RegularExpressions;

namespace DialogueDown.Diagnostics;

/// <summary>
/// The stable definition of one kind of <see cref="Diagnostic"/>: its <see cref="Code"/>
/// (a <c>DLG####</c> identifier), a human <see cref="Title"/>, the <see cref="MessageFormat"/>
/// a renderer fills with a diagnostic's arguments, the <see cref="Category"/> it belongs to,
/// and the <see cref="DefaultSeverity"/> a diagnostic takes unless it overrides it. Many
/// diagnostics share one descriptor, so a descriptor is an immutable value compared by value.
/// </summary>
internal sealed record DiagnosticDescriptor
{
    private const string CodePrefix = "DLG";

    private static readonly Regex _codePattern = new("^DLG[0-9]{4}$");

    public DiagnosticDescriptor(
        string code,
        string title,
        string messageFormat,
        DiagnosticCategory category,
        DiagnosticSeverity defaultSeverity)
    {
        AssertValidCode(code, category);
        Code = code;
        Title = title;
        MessageFormat = messageFormat;
        Category = category;
        DefaultSeverity = defaultSeverity;
    }

    /// <summary>The stable <c>DLG####</c> identifier; its leading digit names its category.</summary>
    public string Code { get; }

    /// <summary>A short human title for the diagnostic kind.</summary>
    public string Title { get; }

    /// <summary>
    /// The composite format a renderer fills with a diagnostic's arguments (for example,
    /// <c>"Unknown speaker '{0}'."</c>). The model keeps the template, not the finished text,
    /// so composing the message stays a rendering concern.
    /// </summary>
    public string MessageFormat { get; }

    /// <summary>The kind of rule this descriptor belongs to; its code range must agree.</summary>
    public DiagnosticCategory Category { get; }

    /// <summary>The severity a diagnostic of this kind takes unless it overrides it.</summary>
    public DiagnosticSeverity DefaultSeverity { get; }

    private static void AssertValidCode(string code, DiagnosticCategory category)
    {
        AssertWellFormedCode(code);
        AssertCodeRangeMatchesCategory(code, category);
    }

    private static void AssertWellFormedCode(string code)
    {
        if (code is null || !_codePattern.IsMatch(code))
        {
            throw new ArgumentException(
                $"Diagnostic code '{code}' is malformed; expected '{CodePrefix}' followed by "
                + "four digits (for example, DLG1001).",
                nameof(code));
        }
    }

    private static void AssertCodeRangeMatchesCategory(string code, DiagnosticCategory category)
    {
        var expected = LeadingDigitFor(category);
        if (code[CodePrefix.Length] != expected)
        {
            throw new ArgumentException(
                $"Diagnostic code '{code}' does not match category {category}; a {category} "
                + $"code must start with '{CodePrefix}{expected}'.",
                nameof(code));
        }
    }

    private static char LeadingDigitFor(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.Syntax => '1',
        DiagnosticCategory.Semantic => '2',
        DiagnosticCategory.Style => '3',
        _ => throw new ArgumentOutOfRangeException(
            nameof(category), category, "Unknown diagnostic category."),
    };
}
