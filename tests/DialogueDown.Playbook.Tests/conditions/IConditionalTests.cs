using System.Reflection;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Playbook.Tests.Conditions;

public sealed class IConditionalTests
{
    [Fact]
    public void IsConditional_SomethingAConditionGuards_IsTrue()
    {
        var guarded = new DivertEdge(1, [], new KeyCondition("Alice.HasKey"));

        Assert.True(guarded.IsConditional());
    }

    [Fact]
    public void IsConditional_SomethingNothingGuards_IsFalse()
    {
        var open = new DivertEdge(1, [], Condition: null);

        Assert.False(open.IsConditional());
    }

    [Fact]
    public void EveryKindCarryingACondition_SaysSoThroughTheInterface()
    {
        // The compiler cannot make a new kind implement this, so nothing but a test stops one
        // arriving with a condition nobody reads. A reader asking through the interface would
        // then find no guard and play the kind as though the world had allowed it.
        var unannounced = ConditionCarryingKinds()
            .Where(kind => !kind.IsAssignableTo(typeof(IConditional)))
            .Select(kind => kind.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unannounced.Count == 0,
            $"{unannounced.Count} kind(s) carry a condition without implementing IConditional: "
                + string.Join(", ", unannounced)
                + ". Declare the interface, so a reader looking for a guard finds this one too.");
    }

    [Fact]
    public void TheKindsCarryingACondition_AreMoreThanAHandful()
    {
        // Guards the guard: a search that stopped finding types would leave the check above
        // passing against nothing.
        Assert.True(ConditionCarryingKinds().Count > 4);
    }

    /// <summary>Every node and edge the format defines that declares a condition of its own.</summary>
    private static List<Type> ConditionCarryingKinds() =>
        typeof(Node).Assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(Node)) || type.IsAssignableTo(typeof(Edge)))
            .Where(type => !type.IsAbstract)
            .Where(type => type.GetProperty(nameof(IConditional.Condition), BindingFlags.Public | BindingFlags.Instance) is not null)
            .ToList();
}
