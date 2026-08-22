namespace DialogueDown.Tests.Support;

/// <summary>
/// The example scripts the repository ships, copied beside the tests by the build so reading one
/// needs no knowledge of where the checkout lives.
/// </summary>
internal static class ExampleScripts
{
    private static string Folder => Path.Combine(AppContext.BaseDirectory, "examples");

    /// <summary>Every example, by file name, in a stable order.</summary>
    /// <returns>The names of the scripts under <c>examples/</c>.</returns>
    public static IEnumerable<string> Names() =>
        Directory.EnumerateFiles(Folder, "*.dialogue.md")
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.Ordinal);

    /// <summary>What one example says.</summary>
    /// <param name="name">The example's file name.</param>
    /// <returns>Its source.</returns>
    public static string Read(string name) => File.ReadAllText(Path.Combine(Folder, name));
}
