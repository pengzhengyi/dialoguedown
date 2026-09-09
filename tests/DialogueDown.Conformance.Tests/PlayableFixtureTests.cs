using System.Text.Json.Nodes;

namespace DialogueDown.Conformance;

public sealed class PlayableFixtureTests
{
    [Fact]
    public void Read_AFixture_CarriesItsFixture()
    {
        var fixture = PlayableFixture.Read("""
            {
              "name": "a playable fixture",
              "playbook": "playbook.json",
              "because": "a reason a reviewer can weigh",
              "session": [
                { "send": "next" },
                { "send": { "choose": 0 } },
                { "expect": { "said": { "speaker": "Alice", "speech": "Hi" } } },
                { "expect": { "ended": {} } }
              ]
            }
            """);

        Assert.Equal("a playable fixture", fixture.Name);
        Assert.Equal("playbook.json", fixture.Playbook);
        Assert.Equal("a reason a reviewer can weigh", fixture.Because);

        Assert.Equal(4, fixture.Session.Length);

        var send1 = Assert.IsType<Send>(fixture.Session[0]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("\"next\""), send1.Message));

        var send2 = Assert.IsType<Send>(fixture.Session[1]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "choose": 0 }"""), send2.Message));

        var expect1 = Assert.IsType<Expect>(fixture.Session[2]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "said": { "speaker": "Alice", "speech": "Hi" } }"""), expect1.Message));

        var expect2 = Assert.IsType<Expect>(fixture.Session[3]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "ended": {} }"""), expect2.Message));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("playbook")]
    [InlineData("because")]
    [InlineData("session")]
    public void Read_AFixtureMissingAField_SaysWhichIsMissing(string missing)
    {
        var error = Assert.Throws<InvalidFixtureException>(() => PlayableFixture.Read(Without(missing)));

        Assert.Contains(missing, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_AMisspelledField_IsRefusedRatherThanIgnored()
    {
        var error = Assert.Throws<InvalidFixtureException>(() => PlayableFixture.Read(With("playbok", "playbook.json")));

        Assert.Contains("playbok", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_AFixturePointingAtItsSchema_KeepsTheUrl()
    {
        var fixture = PlayableFixture.Read(
            With("$schema", "https://pengzhengyi.github.io/dialoguedown/schema/fixture-0.schema.json"));

        Assert.Equal("https://pengzhengyi.github.io/dialoguedown/schema/fixture-0.schema.json", fixture.Schema);
    }

    [Fact]
    public void Read_AFixtureWithoutASchema_IsStillRead()
    {
        Assert.Null(PlayableFixture.Read(Fixture().ToJsonString()).Schema);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[]")]
    public void Read_SomethingThatIsNotAFixture_SaysSo(string json)
    {
        Assert.Throws<InvalidFixtureException>(() => PlayableFixture.Read(json));
    }

    [Fact]
    public void Read_ATurnCarryingBothSendAndExpect_IsRefused()
    {
        var json = With("session", new JsonArray(
            new JsonObject { ["send"] = "next", ["expect"] = "same" }));

        Assert.Throws<InvalidFixtureException>(() => PlayableFixture.Read(json));
    }

    [Fact]
    public void Read_ATurnCarryingNeitherSendNorExpect_IsRefused()
    {
        var json = With("session", new JsonArray(
            new JsonObject { ["unrelated"] = 1 }));

        Assert.Throws<InvalidFixtureException>(() => PlayableFixture.Read(json));
    }

    private static JsonObject Fixture() => new()
    {
        ["name"] = "a fixture",
        ["playbook"] = "playbook.json",
        ["because"] = "a reason a reviewer can weigh",
        ["session"] = new JsonArray(new JsonObject { ["send"] = "next" }),
    };

    private static string With(string field, JsonNode value)
    {
        var fixture = Fixture();
        fixture[field] = value;

        return fixture.ToJsonString();
    }

    private static string Without(string field)
    {
        var fixture = Fixture();

        Assert.True(fixture.Remove(field), $"'{field}' is not a field of a fixture.");

        return fixture.ToJsonString();
    }
}
