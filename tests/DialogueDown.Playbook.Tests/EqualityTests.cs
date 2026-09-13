using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Tests.Support;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Playbook.Tests;

/// <summary>
/// Guards the playbook's value-equality contract: no record keeps a collection it compares by
/// reference, and a whole document still compares by value after it is read back.
/// </summary>
public sealed class EqualityTests
{
    [Fact]
    public void EveryCollectionBearingRecord_HasGeneratedValueEquality()
    {
        // GE001 fails the build when an [Equatable] record leaves a collection property out of its
        // equality. This is the other half: a record that carries a collection but was never marked
        // [Equatable] would compare it by reference, and the analyzer never looks at that type.
        var offenders = typeof(PlaybookDocument).Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsInterface)
            .Where(DeclaresCollection)
            .Where(type => !HasSelfEqualityComparer(type))
            .Select(type => type.Name)
            .Order()
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void AComprehensivePlaybook_ReadTwice_IsOneValue()
    {
        // A document holding every node, edge, weight, condition, and speech kind, read back twice,
        // must be one value both times — which only holds if each construct compares by value.
        var json = PlaybookJsonAssert.Serialize(ComprehensivePlaybook());

        EqualityAssert.AssertValueEqual(
            PlaybookJsonAssert.AssertDeserialize<PlaybookDocument>(json),
            PlaybookJsonAssert.AssertDeserialize<PlaybookDocument>(json));
    }

    private static bool DeclaresCollection(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(property => IsCollection(property.PropertyType));

    private static bool IsCollection(Type type) =>
        type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

    // The generator gives every [Equatable] type a nested IEqualityComparer<TSelf>. Finding that
    // nested comparer is how the test sees generated equality: the [Equatable] attribute is
    // [Conditional] and never reaches the compiled metadata.
    private static bool HasSelfEqualityComparer(Type type) =>
        type.GetNestedTypes(BindingFlags.Public).Any(nested =>
            nested.GetInterfaces().Any(face =>
                face.IsGenericType
                && face.GetGenericTypeDefinition() == typeof(IEqualityComparer<>)
                && face.GetGenericArguments()[0] == type));

    private static PlaybookDocument ComprehensivePlaybook()
    {
        ImmutableArray<SpeechFragment> speech =
        [
            new TextFragment("Welcome"),
            new StyledTextFragment(SpeechStyle.Bold, [new TextFragment("bold")]),
            new LinkFragment("https://example.com", [new TextFragment("the notice")]),
            new ImageFragment("portrait.png", [new TextFragment("Alice")]),
            new LineBreakFragment(),
            new QueryFragment("Alice.FavoriteColor"),
            new DefaultCommandFragment("wait"),
            new CustomCommandFragment("JoinClub", ["Alice"]),
            new TagFragment("mood", "warm", false),
        ];

        return PlaybookFactory.Document(
            anchors: [("the-inn", 5)],
            speakers: [new PlaybookSpeaker("alice", "Alice", false, [new SpeakerTag("mood", "warm", false)])],
            nodes:
            [
                new LineNode(0, 0, speech, new KeyCondition("IsCurious"), [new SuccessionEdge(1)]),
                new ChoiceNode(1, true,
                [
                    new OptionEdge(2, [new TextFragment("Ask about the inn")], null),
                    new RandomOptionEdge(3, new AutoWeight(), null),
                    new RandomOptionEdge(4, new NumberWeight(25), null),
                    new RandomOptionEdge(5, new QueryWeight("Bob.Affection"), null),
                ]),
                new RandomChoiceNode(2, [new SuccessionEdge(3)]),
                new BranchNode(3,
                [
                    new BranchEdge(4, 0, null),
                    new BranchEdge(5, 1, new KeyCondition("HasKey")),
                ]),
                new ControlNode(
                    4,
                    [new CustomCommandFragment("JoinClub", ["Alice"])],
                    null,
                    [new DivertEdge(5, [new TextFragment("the inn")], null)]),
                new EndNode(5),
            ]);
    }
}
