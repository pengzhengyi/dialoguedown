using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace DialogueDown.TestSupport;

/// <summary>Assertions about serialized JSON, compared as JSON rather than searched as text.</summary>
public static class JsonAssert
{
    /// <summary>
    /// Asserts that <paramref name="actual"/> is exactly the JSON <paramref name="expected"/> writes
    /// out, key order included, so a test can show the shape it expects as it would appear on the wire.
    /// </summary>
    /// <remarks>
    /// A missing value and a JSON <c>null</c> both read as <c>null</c>, so expecting <c>"null"</c>
    /// cannot tell them apart; <see cref="AssertOmits"/> says a field is missing.
    /// </remarks>
    /// <param name="expected">The expected value, as JSON text.</param>
    /// <param name="actual">The value read from the serialized output, or <c>null</c> when it is absent.</param>
    public static void AssertJson([StringSyntax(StringSyntaxAttribute.Json)] string expected, JsonNode? actual) =>
        // Both sides are written back the same way, so a difference shows where the values part.
        Assert.Equal(JsonNode.Parse(expected)?.ToJsonString(), actual?.ToJsonString());

    /// <summary>Asserts that a JSON object has no <paramref name="field"/> at all, not even a <c>null</c> one.</summary>
    /// <param name="owner">The object that should leave the field out.</param>
    /// <param name="field">The field's name.</param>
    public static void AssertOmits(JsonNode? owner, string field)
    {
        var json = Assert.IsType<JsonObject>(owner);
        Assert.False(
            json.ContainsKey(field),
            $"Expected no \"{field}\", but found {json[field]?.ToJsonString() ?? "null"}.");
    }
}
