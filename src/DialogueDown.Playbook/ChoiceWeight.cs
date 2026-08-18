using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// How likely a random option is to be drawn.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = PlaybookJson.Discriminator)]
[JsonDerivedType(typeof(AutoWeight), WeightKinds.Auto)]
[JsonDerivedType(typeof(NumberWeight), WeightKinds.Number)]
[JsonDerivedType(typeof(QueryWeight), WeightKinds.Query)]
public abstract record ChoiceWeight
{
    private protected ChoiceWeight()
    {
    }
}
