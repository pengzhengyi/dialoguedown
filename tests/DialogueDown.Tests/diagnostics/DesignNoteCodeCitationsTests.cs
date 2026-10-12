using System.Text.RegularExpressions;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Diagnostics;

/// <summary>
/// Checks that a shipped design note cites only diagnostic codes the catalog can emit.
/// </summary>
/// <remarks>
/// A note whose status is Explored or Proposed is exempt, because it may name codes it means to
/// reserve.
/// </remarks>
public sealed partial class DesignNoteCodeCitationsTests
{
    /// <summary>
    /// Codes a shipped note may name without the catalog containing them, each because the note
    /// itself presents it as unused or as part of an alternative it rejected.
    /// </summary>
    private static readonly Dictionary<string, string> _namedButNotShipped = new(StringComparer.Ordinal)
    {
        ["DLG3001"] = "Choice Nesting Diagnostic names it as a code left unused.",
    };

    [Fact]
    public void ShippedNotes_CodeCitations_ExistInTheCatalog()
    {
        var catalog = DiagnosticCatalogReflection.Descriptors()
            .Select(descriptor => descriptor.Code)
            .ToHashSet(StringComparer.Ordinal);
        var root = Repository.Root;
        var offenders = new List<string>();

        foreach (var path in Directory.EnumerateFiles(
            Path.Combine(root, "docs", "contributing", "design-notes"),
            "*.md",
            SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            if (!IsShipped(text))
            {
                continue;
            }

            offenders.AddRange(Codes().Matches(text)
                .Select(match => match.Value)
                .Where(code => !catalog.Contains(code) && !_namedButNotShipped.ContainsKey(code))
                .Select(code => $"{Path.GetRelativePath(root, path)}: {code}"));
        }

        // Assert.True rather than Assert.Empty: xUnit shortens each entry it prints, which would cut
        // off the code at the end of a note's path.
        Assert.True(
            offenders.Count == 0,
            "A design note that ships cites a code the catalog cannot emit, so a reader who looks it "
            + "up finds nothing. Either the note is stale or the code is a reservation it should mark "
            + "as one:\n  " + string.Join("\n  ", offenders)
            + "\nExempt by design: "
            + string.Join("; ", _namedButNotShipped.Select(entry => $"{entry.Key} ({entry.Value})")));
    }

    /// <summary>
    /// A note is shipped when it has a status line that is neither Explored nor Proposed.
    /// </summary>
    private static bool IsShipped(string text)
    {
        var status = Status().Match(text);
        if (!status.Success)
        {
            return false;
        }

        var line = status.Groups[1].Value;
        return !line.Contains("Explored", StringComparison.OrdinalIgnoreCase)
            && !line.Contains("Proposed", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^> Status:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex Status();

    [GeneratedRegex(@"DLG\d{4}")]
    private static partial Regex Codes();
}
