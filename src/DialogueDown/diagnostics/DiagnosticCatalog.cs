namespace DialogueDown.Diagnostics;

/// <summary>
/// The central inventory of every diagnostic kind DialogueDown reports. Keeping the descriptors in
/// one place makes the <c>DLG####</c> codes greppable and documentable, and lets a test enforce
/// that each code is unique — a guarantee scattered per-producer descriptors cannot give. A
/// producer or validation rule reports by referencing the descriptor it owns here.
/// </summary>
internal static class DiagnosticCatalog
{
    // Syntax — DLG1xxx: a malformed line surface, or a structural readability concern.

    /// <summary>DLG1003 — a line carries more than one jump (structural, advisory).</summary>
    public static readonly DiagnosticDescriptor MultipleJumpsOnLine = new(
        "DLG1003",
        "Multiple jumps on a line",
        "This line has {0} jumps; multiple jumps on one line run in sequence and are easy to "
            + "misread — prefer at most one.",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Warning);

    /// <summary>DLG1101 — a speaker prefix binds tags but names no speaker.</summary>
    public static readonly DiagnosticDescriptor TagsWithoutSpeaker = new(
        "DLG1101",
        "Tags without a speaker",
        "\"{0}\" has tags but names no speaker for them to attach to. Begin the line with a name "
            + "to declare a speaker (Alice #excited:), or with an @id to add tags to an "
            + "already-declared one (@alice #excited:).",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    /// <summary>DLG1102 — a code span is not a valid game call.</summary>
    public static readonly DiagnosticDescriptor NotAGameCall = new(
        "DLG1102",
        "Not a game call",
        "\"{0}\" is not a game call. Write a query that reads a value (\"key\"), a default command "
            + "((\"do something\")), or a named command (Name(\"arg\", ...)).",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    /// <summary>DLG1103 — a functional element appears inside a label or alt text.</summary>
    public static readonly DiagnosticDescriptor DisallowedLabelElement = new(
        "DLG1103",
        "Disallowed element in a label",
        "{0} is not allowed inside a label or alt text; only text and styling are.",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    /// <summary>DLG1104 — an option in a random choice carries no weight.</summary>
    public static readonly DiagnosticDescriptor MissingChoiceWeight = new(
        "DLG1104",
        "Missing weight in a random choice",
        "This option has no weight, but its list is a random choice. Give it a weight like `50%`, "
            + "or `%` to share the remaining percentage equally.",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    /// <summary>DLG1105 — a choice weight is neither a non-negative number nor a bare percent.</summary>
    public static readonly DiagnosticDescriptor InvalidChoiceWeight = new(
        "DLG1105",
        "Invalid choice weight",
        "\"{0}\" is not a valid weight. Write a non-negative percentage like `50%`, or `%` to "
            + "share the remaining percentage equally.",
        DiagnosticCategory.Syntax,
        DiagnosticSeverity.Error);

    // Semantic — DLG2xxx: a meaning-level conflict found during analysis.

    /// <summary>DLG2001 — two headings slug to the same anchor.</summary>
    public static readonly DiagnosticDescriptor DuplicateAnchor = new(
        "DLG2001",
        "Duplicate scene anchor",
        "Two scenes resolve to the same anchor '#{0}'. Rename one heading so each jump target is "
            + "unambiguous.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2002 — a heading has no sluggable text to form an anchor.</summary>
    public static readonly DiagnosticDescriptor HeadingWithoutAnchor = new(
        "DLG2002",
        "Heading without an anchor",
        "A heading needs at least one letter or number so it can be a jump target; this one has "
            + "none. Add sluggable text to the heading.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2003 — a name and an <c>@id</c> already name different speakers.</summary>
    public static readonly DiagnosticDescriptor SpeakerNameIdConflict = new(
        "DLG2003",
        "Ambiguous speaker binding",
        "Cannot bind name '{0}' to id '@{1}': both are already in use as separate speakers, so "
            + "joining them now is ambiguous. If they are the same speaker, declare it "
            + "(Name @{1}: …) before either is used on its own.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2004 — an <c>@id</c> is already bound to another speaker name.</summary>
    public static readonly DiagnosticDescriptor IdBoundToAnotherName = new(
        "DLG2004",
        "Id bound to two names",
        "id '@{0}' is already bound to speaker '{1}', so it cannot also be bound to '{2}'. Use a "
            + "different id for '{2}'.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2005 — a speaker name is already bound to another <c>@id</c>.</summary>
    public static readonly DiagnosticDescriptor NameBoundToAnotherId = new(
        "DLG2005",
        "Name bound to two ids",
        "Speaker '{0}' is already bound to id '@{1}', so it cannot also be bound to id '@{2}'. "
            + "Give the speaker a single id.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2006 — more than one speaker is marked <c>##default</c>.</summary>
    public static readonly DiagnosticDescriptor MultipleDefaultSpeakers = new(
        "DLG2006",
        "More than one default speaker",
        "Two speakers are marked ##default ('{0}' and '{1}'); only one default speaker is allowed.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2007 — a stable <c>@id</c> is used but never given a name.</summary>
    public static readonly DiagnosticDescriptor UnnamedSpeakerId = new(
        "DLG2007",
        "Unnamed speaker id",
        "Speaker '@{0}' is used but never declared with a name. Declare it with a name "
            + "(Name @{0}: …) — a stable id must belong to a named speaker.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2008 — a <c>##reserved</c> tag name is not one DialogueDown knows.</summary>
    public static readonly DiagnosticDescriptor UnknownReservedTag = new(
        "DLG2008",
        "Unknown reserved tag",
        "'##{0}' is not a known reserved tag. Use a custom tag ('#{0}') or one of DialogueDown's "
            + "reserved tags.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2009 — a jump targets a local anchor that no scene owns.</summary>
    public static readonly DiagnosticDescriptor MissingScene = new(
        "DLG2009",
        "Jump to a missing scene",
        "Jump target '#{0}' does not match any scene. Check the anchor, or add a heading it can "
            + "point to.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    /// <summary>DLG2010 — every weight in a random choice resolves to zero.</summary>
    public static readonly DiagnosticDescriptor ZeroChoiceWeightTotal = new(
        "DLG2010",
        "Random choice weights sum to zero",
        "Every weight in this random choice is 0, so no option can be selected. Give at least one "
            + "option a positive weight.",
        DiagnosticCategory.Semantic,
        DiagnosticSeverity.Error);

    // Style — DLG3xxx: a valid script shape that may be difficult to read or maintain.

    /// <summary>DLG3002 — a choice branch exceeds the recommended nesting depth.</summary>
    public static readonly DiagnosticDescriptor DeeplyNestedChoiceBranch = new(
        "DLG3002",
        "Deeply nested choice branch",
        "This branch reaches choice nesting level {0}; the recommended maximum is {1}. Consider "
            + "moving this branch into a new scene and jumping to it instead.",
        DiagnosticCategory.Style,
        DiagnosticSeverity.Warning);

    /// <summary>DLG3003 — a random choice's static weights do not total 100%.</summary>
    public static readonly DiagnosticDescriptor ChoiceWeightsNotOneHundred = new(
        "DLG3003",
        "Choice weights do not total 100%",
        "These weights total {0}%, not 100%. Weights are normalized by their sum, so the odds "
            + "still work; adjust them to total 100% to state the intended odds directly.",
        DiagnosticCategory.Style,
        DiagnosticSeverity.Warning);

    /// <summary>DLG3004 — a random choice offers only one option.</summary>
    public static readonly DiagnosticDescriptor SingleOptionRandomChoice = new(
        "DLG3004",
        "Single-option random choice",
        "This random choice has a single option, so it is always selected and the weight has no "
            + "effect. Remove the weight to make it a plain line, or add more options.",
        DiagnosticCategory.Style,
        DiagnosticSeverity.Warning);
}
