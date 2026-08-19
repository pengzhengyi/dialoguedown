namespace DialogueDown.Visualization.Render;

/// <summary>
/// Reads a bundled client asset embedded in this assembly — the single
/// self-contained report page built by the <c>web/</c> Vite project — so a
/// generated report needs no files on disk.
/// </summary>
internal static class EmbeddedAsset
{
    public static string ReadText(string fileName)
    {
        var assembly = typeof(EmbeddedAsset).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("." + fileName, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded asset '{fileName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
