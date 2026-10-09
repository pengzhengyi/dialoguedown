using System.Text.Json.Nodes;
using DialogueDown.Conformance.Tests.Support;
using static DialogueDown.Conformance.Tests.Support.FixtureJsonFactory;
using static DialogueDown.Conformance.Tests.Support.InvalidFixtureAssert;

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
        AssertInvalid(() => PlayableFixture.Read(APlayableFixture().WithoutField(missing)), missing);
    }

    [Fact]
    public void Read_AMisspelledField_IsRefusedRatherThanIgnored()
    {
        AssertInvalid(() => PlayableFixture.Read(APlayableFixture().WithField("playbok", "playbook.json")), "playbok");
    }

    [Fact]
    public void Read_AFixturePointingAtItsSchema_KeepsTheUrl()
    {
        var fixture = PlayableFixture.Read(
            APlayableFixture().WithField("$schema", SchemaUrl));

        Assert.Equal(SchemaUrl, fixture.Schema);
    }

    [Fact]
    public void Read_AFixtureWithoutASchema_IsStillRead()
    {
        Assert.Null(PlayableFixture.Read(APlayableFixture().ToJsonString()).Schema);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[]")]
    public void Read_SomethingThatIsNotAFixture_SaysSo(string json)
    {
        AssertInvalid(() => PlayableFixture.Read(json));
    }

    [Fact]
    public void Read_ASessionEntryCarryingBothSendAndExpect_IsRefused()
    {
        var json = APlayableFixture().WithField("session", new JsonArray(
            new JsonObject { ["send"] = "next", ["expect"] = "same" }));

        AssertInvalid(() => PlayableFixture.Read(json));
    }

    [Fact]
    public void Read_ASessionEntryCarryingNeitherSendNorExpect_IsRefused()
    {
        var json = APlayableFixture().WithField("session", new JsonArray(
            new JsonObject { ["unrelated"] = 1 }));

        AssertInvalid(() => PlayableFixture.Read(json));
    }
}
