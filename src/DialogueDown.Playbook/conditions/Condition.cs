using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// A question that decides whether a line plays, an option is offered, or a jump is taken.
/// </summary>
/// <remarks>
/// Written as a tagged object, such as <c>{ "kind": "key", "key": "Alice.HasKey" }</c>, so a new
/// kind of condition can be added without changing how the existing ones are written.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = PlaybookJson.Discriminator)]
[JsonDerivedType(typeof(KeyCondition), ConditionKinds.Key)]
public abstract record Condition
{
    private protected Condition()
    {
    }
}
