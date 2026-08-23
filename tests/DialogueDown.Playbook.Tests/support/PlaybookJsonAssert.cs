using System.Text.Json;

namespace DialogueDown.Playbook.Tests.Support;

/// <summary>
/// Assertions over the playbook's canonical JSON encoding, so a test states what it means —
/// "this document survives a round trip" — rather than repeating the serializer call.
/// </summary>
/// <remarks>
/// Every method uses <see cref="PlaybookJson.Options"/>. Reaching for the serializer directly in
/// a test would let it drift onto settings the format does not use, and quietly prove nothing.
/// </remarks>
internal static class PlaybookJsonAssert
{
    /// <summary>Writes a value with the format's settings.</summary>
    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, PlaybookJson.Options);

    /// <summary>Reads a document, asserting it produced a value.</summary>
    public static T AssertDeserialize<T>(string json)
    {
        var value = JsonSerializer.Deserialize<T>(json, PlaybookJson.Options);

        Assert.NotNull(value);
        return value;
    }

    /// <summary>Asserts a value writes as exactly the expected document.</summary>
    public static void AssertSerialized<T>(string expectedJson, T value) =>
        Assert.Equal(expectedJson, Serialize(value));

    /// <summary>
    /// Asserts a document survives being read and written unchanged, and returns what it read.
    /// </summary>
    public static T AssertRoundTrip<T>(string json)
    {
        var value = AssertDeserialize<T>(json);

        AssertSerialized(json, value);
        return value;
    }

    /// <summary>
    /// The same, for one member of a union: the document is read as <typeparamref name="TDeclared"/>
    /// and must arrive as <typeparamref name="TActual"/>, which proves the discriminator resolved.
    /// </summary>
    public static TActual AssertRoundTrip<TDeclared, TActual>(string json)
        where TActual : TDeclared =>
        Assert.IsType<TActual>(AssertRoundTrip<TDeclared>(json));

    /// <summary>Asserts the reader refuses a document, and returns why.</summary>
    public static JsonException AssertRefuses<T>(string json) =>
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<T>(json, PlaybookJson.Options));
}
