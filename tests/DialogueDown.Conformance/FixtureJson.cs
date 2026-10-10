using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Conformance;

/// <summary>How a fixture's JSON is read.</summary>
internal static class FixtureJson
{
    /// <summary>The options for reading one kind of fixture.</summary>
    /// <param name="converter">The converter for the part of the fixture that is specific to its kind.</param>
    /// <returns>Options that read camel-case fields and refuse any field the fixture does not declare.</returns>
    public static JsonSerializerOptions OptionsWith(JsonConverter converter) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // A fixture is written by hand, so a misspelled field is reported rather than ignored.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { converter },
    };
}
