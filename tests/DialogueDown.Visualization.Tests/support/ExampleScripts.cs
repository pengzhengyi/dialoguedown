namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// The example scripts the repository ships, copied beside the tests by the build so reading one
/// needs no knowledge of where the checkout lives.
/// </summary>
internal static class ExampleScripts
{
    private static string Folder => Path.Combine(AppContext.BaseDirectory, "examples");

    /// <summary>Every example, by file name, in a stable order.</summary>
    public static IEnumerable<string> Names() =>
        Directory.EnumerateFiles(Folder, "*.dialogue.md")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal);

    /// <summary>What one example says.</summary>
    public static string Read(string name) => File.ReadAllText(Path.Combine(Folder, name));
}
