using DialogueDown.Script.Validation;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds a <see cref="StructuralValidator"/>, so a test that exercises the pass
/// itself — not a particular rule — builds a rule-free validator through one place.
/// </summary>
internal static class StructuralValidatorFactory
{
    /// <summary>A validator with no rules, for tests that only exercise the pass.</summary>
    public static StructuralValidator WithoutRules() => new([]);
}
