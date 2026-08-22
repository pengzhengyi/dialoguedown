using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// A question that decides whether a line plays, an option is offered, or a jump fires.
/// </summary>
/// <remarks>
/// A tagged object rather than a bare string, so negation and composition can be added as new
/// kinds without changing what every existing condition looks like.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = PlaybookJson.Discriminator)]
[JsonDerivedType(typeof(KeyCondition), ConditionKinds.Key)]
public abstract record Condition
{
    private protected Condition()
    {
    }
}
