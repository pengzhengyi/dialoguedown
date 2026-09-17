using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DialogueDown.Tests.Diagnostics;

/// <summary>
/// Guards that a design note which ships cites only diagnostic codes the catalog can emit. An
/// origin document is the likeliest place for a code to go stale — one implemented note still
/// assigned the dangling arrow to <c>DLG1002</c> while the shipped code was <c>DLG1113</c> — so
/// the citations in shipped notes are checked against the catalog. A note whose status is
/// Explored or Proposed is exempt: it names codes it means to reserve.
/// </summary>
public sealed partial class DesignNoteCodeCitationsTests
{
    /// <summary>
    /// Codes a shipped note may name without the catalog containing them, each because the note
    /// itself presents it as unused or as part of an alternative it rejected.
    /// </summary>
    private static readonly Dictionary<string, string> _namedButNotShipped = new(StringComparer.Ordinal)
    {
        ["DLG1115"] = "Ignored Markdown Diagnostic names it in a rejected alternative.",
        ["DLG3001"] = "Choice Nesting Diagnostic names it as a code left unused.",
    };

    [Fact]
    public void ShippedNotes_CodeCitations_ExistInTheCatalog()
    {
        var catalog = DiagnosticCatalogReflection.Descriptors()
            .Select(descriptor => descriptor.Code)
            .ToHashSet(StringComparer.Ordinal);
        var root = RepositoryRoot();
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

        Assert.True(
            offenders.Count == 0,
            "A design note that ships cites a code the catalog cannot emit, so a reader who looks it "
            + "up finds nothing. Either the note is stale or the code is a reservation it should mark "
            + "as one:\n  " + string.Join("\n  ", offenders)
            + "\nExempt by design: "
            + string.Join("; ", _namedButNotShipped.Select(entry => $"{entry.Key} ({entry.Value})")));
    }

    /// <summary>A note is shipped unless its status callout still calls it a proposal.</summary>
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

    // Walks up from this test's source location to the checkout root, identified by its solution
    // file, so the notes are found wherever the runner places the compiled binaries.
    private static string RepositoryRoot([CallerFilePath] string callerPath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(callerPath)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DialogueDown.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find the repository root (DialogueDown.sln).");
    }

    [GeneratedRegex(@"^> Status:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex Status();

    [GeneratedRegex(@"DLG\d{4}")]
    private static partial Regex Codes();
}
