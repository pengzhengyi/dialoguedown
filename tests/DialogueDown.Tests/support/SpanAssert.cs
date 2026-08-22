using DialogueDown.Common;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Assertions over the <see cref="SourceSpan"/> a stage attaches to what it produces, so a test
/// states what a span promises instead of repeating the arithmetic that checks it.
/// </summary>
internal static class SpanAssert
{
    /// <summary>
    /// Asserts <paramref name="span"/> addresses text that exists in <paramref name="source"/> —
    /// that slicing the source by it yields the text the span stands for, rather than throwing.
    /// <paramref name="subject"/> names what carries the span, so a failure says what is at fault.
    /// </summary>
    public static void AssertAddressesTextThatExists(
        SourceSpan span, string source, string subject)
    {
        ArgumentNullException.ThrowIfNull(source);

        Assert.True(
            span.Start >= 0 && span.End >= span.Start && span.End <= source.Length,
            ReachesOutsideTheSource(subject, span, source));
    }

    /// <summary>
    /// Asserts <paramref name="child"/> claims no text outside <paramref name="parent"/>, which is
    /// what lets a tree be searched by position: a walk that descends into the child containing a
    /// position never has to look outside a parent to find what is there.
    /// </summary>
    public static void AssertContainedIn(
        SourceSpan child, SourceSpan parent, string childSubject, string parentSubject) =>
        Assert.True(
            child.Start >= parent.Start && child.End <= parent.End,
            EscapesItsParent(childSubject, child, parentSubject, parent));

    private static string ReachesOutsideTheSource(
        string subject, SourceSpan span, string source) =>
        $"{subject} claims [{span.Start}, {span.End}) of a {source.Length}-character source.";

    private static string EscapesItsParent(
        string childSubject, SourceSpan child, string parentSubject, SourceSpan parent) =>
        $"{childSubject} claims [{child.Start}, {child.End}), escaping its parent "
        + $"{parentSubject}, which claims only [{parent.Start}, {parent.End}).";
}
