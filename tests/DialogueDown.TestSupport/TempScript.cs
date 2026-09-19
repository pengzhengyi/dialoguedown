namespace DialogueDown.TestSupport;

/// <summary>
/// A temporary <c>.dialogue.md</c> script that deletes itself on dispose.
/// </summary>
/// <remarks>
/// Some tests need a script that exists on disk, because what they check is a path being read,
/// watched, or passed to a command. The file goes in the system temp folder under a name no other
/// test can collide with, and disposing removes it.
/// </remarks>
public sealed class TempScript : IDisposable
{
    /// <summary>Writes the script.</summary>
    /// <param name="content">What the script says.</param>
    public TempScript(string content)
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"dd-script-{Guid.NewGuid():N}.dialogue.md");
        File.WriteAllText(Path, content);
    }

    /// <summary>Gets the absolute path of the temporary script.</summary>
    public string Path { get; }

    /// <summary>Deletes the script.</summary>
    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
