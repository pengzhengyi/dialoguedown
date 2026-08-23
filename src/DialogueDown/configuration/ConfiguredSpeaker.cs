using System.Collections.Immutable;
using DialogueDown.Common;
using Generator.Equals;

namespace DialogueDown.Configuration;

/// <summary>
/// A speaker supplied by configuration rather than declared in a script: a display name, an
/// optional stable id, and its tags partitioned into immutable custom and reserved sequences. It
/// is plain data, validated and partitioned at the configuration edge; the semantic stage turns it
/// into a speaker declaration to bind alongside the script's own speakers. A speaker is the
/// default when its <see cref="ReservedTags"/> include the <c>default</c> reserved tag.
/// </summary>
[Equatable]
public sealed partial record ConfiguredSpeaker
{
    [IgnoreEquality]
    private ImmutableArray<ConfiguredTag> _customTags = ImmutableArray<ConfiguredTag>.Empty;

    [IgnoreEquality]
    private ImmutableArray<ConfiguredTag> _reservedTags = ImmutableArray<ConfiguredTag>.Empty;

    /// <summary>Creates an immutable snapshot of a configured speaker and its tag sequences.</summary>
    public ConfiguredSpeaker(
        string name,
        string? id,
        IEnumerable<ConfiguredTag> customTags,
        IEnumerable<ConfiguredTag> reservedTags)
    {
        ArgumentNullException.ThrowIfNull(customTags);
        ArgumentNullException.ThrowIfNull(reservedTags);

        Name = name;
        Id = id;
        CustomTags = customTags.ToImmutableArray();
        ReservedTags = reservedTags.ToImmutableArray();
    }

    /// <summary>The speaker's display name.</summary>
    public string Name { get; init; }

    /// <summary>The optional stable id the script references as <c>@id</c>.</summary>
    public string? Id { get; init; }

    /// <summary>The custom tags in configured order.</summary>
    [OrderedEquality]
    public ImmutableArray<ConfiguredTag> CustomTags
    {
        get => _customTags;
        init => _customTags = value.AssertInitialized(nameof(CustomTags));
    }

    /// <summary>The reserved tags in configured order.</summary>
    [OrderedEquality]
    public ImmutableArray<ConfiguredTag> ReservedTags
    {
        get => _reservedTags;
        init => _reservedTags = value.AssertInitialized(nameof(ReservedTags));
    }

    /// <summary>Deconstructs the speaker into the same four parts as the original positional record.</summary>
    public void Deconstruct(
        out string name,
        out string? id,
        out ImmutableArray<ConfiguredTag> customTags,
        out ImmutableArray<ConfiguredTag> reservedTags)
    {
        name = Name;
        id = Id;
        customTags = CustomTags;
        reservedTags = ReservedTags;
    }

}
