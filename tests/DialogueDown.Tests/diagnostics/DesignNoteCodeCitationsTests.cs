using System.Text.RegularExpressions;
using DialogueDown.Diagnostics;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Diagnostics;

/// <summary>
/// Checks that a shipped design note cites only diagnostic codes the catalog can emit, and that
/// every exemption from that rule is still needed.
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

    private static readonly HashSet<string> _catalog = DiagnosticCatalog.All
        .Select(descriptor => descriptor.Code)
        .ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void ShippedNotes_CodeCitations_ExistInTheCatalog()
    {
        var offenders = ShippedCitations()
            .Where(citation => !_catalog.Contains(citation.Code) && !_namedButNotShipped.ContainsKey(citation.Code))
            .Select(citation => $"{citation.Note}: {citation.Code}")
            .ToList();

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

    [Fact]
    public void Exemptions_AreStillNeeded()
    {
        var cited = ShippedCitations().Select(citation => citation.Code).ToHashSet(StringComparer.Ordinal);

        // An exemption is needed while a shipped note cites its code and the catalog cannot emit
        // it. One that outlives either would quietly excuse the next stale citation of that code.
        Assert.All(
            _namedButNotShipped.Keys,
            code =>
            {
                Assert.Contains(code, cited);
                Assert.DoesNotContain(code, _catalog);
            });
    }

    /// <summary>
    /// Every code a shipped note cites, beside the note's path from the repository root.
    /// </summary>
    private static IEnumerable<(string Note, string Code)> ShippedCitations()
    {
        var root = Repository.Root;
        var notes = Directory.EnumerateFiles(
            Path.Combine(root, "docs", "contributing", "design-notes"), "*.md", SearchOption.AllDirectories);

        foreach (var path in notes)
        {
            var text = File.ReadAllText(path);
            if (!IsShipped(text))
            {
                continue;
            }

            foreach (Match match in Codes().Matches(text))
            {
                yield return (Path.GetRelativePath(root, path), match.Value);
            }
        }
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
