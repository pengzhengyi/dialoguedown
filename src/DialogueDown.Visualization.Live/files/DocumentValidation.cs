namespace DialogueDown.Visualization.Live.Files;

/// <summary>Validates that a CLI file argument is an existing DialogueDown document.</summary>
internal static class DocumentValidation
{
    /// <summary>The required extension for a DialogueDown script.</summary>
    public const string Extension = ".dialogue.md";

    /// <summary>
    /// Returns a human-readable error if <paramref name="file"/> is not a usable
    /// document (missing, or the wrong extension), or <c>null</c> if it is valid.
    /// </summary>
    public static string? Validate(string file)
    {
        if (!file.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            return $"Expected a DialogueDown script ending in '{Extension}': {file}";
        }

        if (!File.Exists(file))
        {
            return $"File not found: {file}";
        }

        return null;
    }
}
