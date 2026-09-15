using System.Text.Json;
using DialogueDown.Playbook;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>
/// How a playbook value is written when it is compared with what a fixture claims.
/// </summary>
/// <remarks>
/// The playbook's own options, so the comparison is between a value and a value rather than
/// between two spellings of one: a fixture may order a property as it likes and may spell out what
/// a writer leaves off. Indentation comes off because nothing here is read by a person — these
/// strings exist to be compared, and a difference in whitespace is not a difference in meaning.
/// </remarks>
internal static class FixtureJson
{
    /// <summary>Writing suited to comparison rather than to reading.</summary>
    public static readonly JsonSerializerOptions Compact =
        new(PlaybookJson.Options) { WriteIndented = false };
}
