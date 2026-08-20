namespace DialogueDown.Visualization.Render;

/// <summary>
/// Whether a script asks the report to draw a Mermaid diagram. An exported report inlines
/// Mermaid's build only for a script that does, because that build is larger than the rest of the
/// report put together and almost no script needs it.
/// </summary>
/// <remarks>
/// This reads the fence's info string the way the client's preview does — the first word,
/// lowercased — but deliberately does not track whether a fence is itself inside another one.
/// Answering "yes" too often only makes an exported file bigger; answering "no" too often would
/// export a diagram that cannot draw, so the doubtful cases answer yes.
/// </remarks>
internal static class MermaidFence
{
    public static bool AppearsIn(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        foreach (var line in source.AsSpan().EnumerateLines())
        {
            if (OpensAMermaidFence(line))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OpensAMermaidFence(ReadOnlySpan<char> line)
    {
        var text = line.TrimStart(' ');
        // More than three spaces of indent makes it an indented code block, not a fence.
        if (line.Length - text.Length > 3)
        {
            return false;
        }

        var fence = text.Length > 0 && (text[0] == '`' || text[0] == '~') ? text[0] : '\0';
        if (fence == '\0')
        {
            return false;
        }

        var marks = 0;
        while (marks < text.Length && text[marks] == fence)
        {
            marks++;
        }

        if (marks < 3)
        {
            return false;
        }

        var info = text[marks..].Trim();
        var word = info.IndexOfAny(' ', '\t') is var space && space >= 0 ? info[..space] : info;
        return word.Equals("mermaid", StringComparison.OrdinalIgnoreCase);
    }
}
