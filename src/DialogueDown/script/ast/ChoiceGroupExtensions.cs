namespace DialogueDown.Script.Ast;

/// <summary>
/// Queries over a <see cref="ChoiceGroup"/> that read the same shape from a player choice and a
/// random choice, whose options differ in type but not in structure.
/// </summary>
internal static class ChoiceGroupExtensions
{
    /// <summary>The body each option leads to, in the order the options are written.</summary>
    public static IReadOnlyList<IReadOnlyList<ScriptBlock>> OptionBodies(this ChoiceGroup group) =>
        group switch
        {
            Choices choices => [.. choices.Options.Select(option => option.Body)],
            RandomChoices random => [.. random.Options.Select(option => option.Body)],
            _ => throw new ArgumentOutOfRangeException(
                nameof(group), group.GetType(), "Unhandled choice group in OptionBodies()."),
        };
}
