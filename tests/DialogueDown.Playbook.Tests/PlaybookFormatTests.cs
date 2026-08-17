using System.Collections.Immutable;
using System.Text.Json;

namespace DialogueDown.Playbook.Tests;

public sealed class PlaybookFormatTests
{
    [Fact]
    public void RoundTrip_HeaderWithCapabilities_PreservesTheDocument()
    {
        const string Json = """
            {
              "version": 0,
              "requires": [
                "core"
              ],
              "uses": [
                "source-map"
              ]
            }
            """;

        var restored = JsonSerializer.Deserialize<PlaybookFormat>(Json, PlaybookJson.Options);

        Assert.Equal(Json, JsonSerializer.Serialize(restored, PlaybookJson.Options));
    }

    [Fact]
    public void Read_HeaderWithoutOptionalCapabilities_TreatsThemAsEmpty()
    {
        const string Json = """{ "version": 0 }""";

        var format = JsonSerializer.Deserialize<PlaybookFormat>(Json, PlaybookJson.Options);

        Assert.Equal(0, format!.Version);
        Assert.Empty(format.Requires);
        Assert.Empty(format.Uses);
    }

    [Fact]
    public void Construct_NegativeVersion_IsRejected()
    {
        void NegativeVersion() =>
            _ = new PlaybookFormat(-1, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

        var error = Assert.Throws<ArgumentOutOfRangeException>(NegativeVersion);
        Assert.Contains("-1", error.Message, StringComparison.Ordinal);
    }
}
