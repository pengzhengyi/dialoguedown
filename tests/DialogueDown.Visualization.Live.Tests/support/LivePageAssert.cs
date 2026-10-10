using DialogueDown.TestSupport;

namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>Assertions about the report pages a live session serves.</summary>
internal static class LivePageAssert
{
    /// <summary>Asserts that a page embeds exactly the report <paramref name="documentJson"/> describes.</summary>
    /// <param name="html">The page.</param>
    /// <param name="documentJson">The document the page should carry, as the session serializes it.</param>
    public static void AssertPageEmbeds(string html, string documentJson) =>
        // Both sides are written back the same way, so a difference shows where the reports part.
        Assert.Equal(
            ReportPayload.Parse(documentJson).Json.ToJsonString(),
            ReportPayload.FromPage(html).Json.ToJsonString());
}
