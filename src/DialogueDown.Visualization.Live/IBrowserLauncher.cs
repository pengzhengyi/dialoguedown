namespace DialogueDown.Visualization.Live;

/// <summary>Opens a URL or file with the user's default application (browser).</summary>
public interface IBrowserLauncher
{
    /// <summary>Opens <paramref name="target"/> (a file path or URL).</summary>
    void Open(string target);
}
