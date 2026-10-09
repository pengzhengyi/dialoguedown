using System.Text.Json.Nodes;

namespace DialogueDown.Conformance.Tests.Support;

/// <summary>
/// The fixtures a corpus case holds, and the playbook they name, built in one place so a test
/// changes only the field it is about.
/// </summary>
internal static class FixtureJsonFactory
{
    /// <summary>The address of the fixture schema, which a fixture may name in <c>$schema</c>.</summary>
    public const string SchemaUrl = "https://pengzhengyi.github.io/dialoguedown/schema/fixture-0.schema.json";

    /// <summary>The smallest playbook a reader opens: version 0, requiring and using nothing.</summary>
    public const string APlaybook = """
        { "format": { "version": 0, "requires": [], "uses": [] } }
        """;

    /// <summary>A well-formed readable fixture: a refusal of <c>playbook.json</c>.</summary>
    /// <remarks>
    /// <code>
    /// {
    ///   "name": "a fixture",
    ///   "playbook": "playbook.json",
    ///   "verdict": "refuse",
    ///   "because": "a reason a reviewer can weigh"
    /// }
    /// </code>
    /// </remarks>
    /// <returns>A fresh fixture a test may change.</returns>
    public static JsonObject AReadableFixture() => new()
    {
        ["name"] = "a fixture",
        ["playbook"] = "playbook.json",
        ["verdict"] = "refuse",
        ["because"] = "a reason a reviewer can weigh",
    };

    /// <summary>A well-formed playable fixture whose session sends <c>next</c> once.</summary>
    /// <remarks>
    /// <code>
    /// {
    ///   "name": "a fixture",
    ///   "playbook": "playbook.json",
    ///   "because": "a reason a reviewer can weigh",
    ///   "session": [{ "send": "next" }]
    /// }
    /// </code>
    /// </remarks>
    /// <returns>A fresh fixture a test may change.</returns>
    public static JsonObject APlayableFixture() => new()
    {
        ["name"] = "a fixture",
        ["playbook"] = "playbook.json",
        ["because"] = "a reason a reviewer can weigh",
        ["session"] = new JsonArray(new JsonObject { ["send"] = "next" }),
    };

    /// <summary>The fixture with one field set, whether or not the field belongs in a fixture.</summary>
    /// <param name="fixture">The fixture to start from; it is left unchanged.</param>
    /// <param name="field">The field to set.</param>
    /// <param name="value">What it is set to.</param>
    /// <returns>The changed fixture as JSON text.</returns>
    public static string WithField(this JsonObject fixture, string field, JsonNode value)
    {
        var changed = fixture.DeepClone().AsObject();
        changed[field] = value;

        return changed.ToJsonString();
    }

    /// <summary>The fixture with one field removed.</summary>
    /// <param name="fixture">The fixture to start from; it is left unchanged.</param>
    /// <param name="field">A field the fixture has.</param>
    /// <returns>The changed fixture as JSON text.</returns>
    public static string WithoutField(this JsonObject fixture, string field)
    {
        var changed = fixture.DeepClone().AsObject();

        // Removing a field the fixture lacks would leave the test checking a complete fixture.
        Assert.True(changed.Remove(field), $"'{field}' is not a field of the fixture.");

        return changed.ToJsonString();
    }
}
