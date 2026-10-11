namespace DialogueDown.Tests.Support;

/// <summary>Where a generated label stands in its script.</summary>
internal enum LabelPlacement
{
    /// <summary>A link in a line: <c>Alice: See [label](#the-inn).</c></summary>
    Link,

    /// <summary>An image's alt text: <c>Alice: ![label](a.png)</c></summary>
    AltText,

    /// <summary>A menu option written as a jump: <c>- =&gt; [label](#the-inn)</c></summary>
    MenuOption,

    /// <summary>A jump at the end of a line: <c>Alice: This way. =&gt; [label](#the-inn)</c></summary>
    Divert,
}

/// <summary>The kind of code span a generated label holds.</summary>
internal enum LabelCallKind
{
    Query,
    Command,
    Condition,
}

/// <summary>A generated script holding one label, and what the generator placed in it.</summary>
/// <param name="Source">The whole script.</param>
/// <param name="Placement">Where the label stands.</param>
/// <param name="Label">The text between the label's brackets.</param>
/// <param name="Calls">Each code span in the label, in the order written.</param>
internal sealed record LabelScript(
    string Source, LabelPlacement Placement, string Label, IReadOnlyList<LabelCall> Calls);

/// <summary>A code span written into a generated label.</summary>
/// <param name="Kind">What the code span is.</param>
/// <param name="Written">The code span as the source spells it, backticks included.</param>
/// <param name="Key">The key a query reads or a condition tests, or the action a command names.</param>
internal sealed record LabelCall(LabelCallKind Kind, string Written, string Key);
