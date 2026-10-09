using System.Text.Json.Nodes;
using static DialogueDown.Conformance.Tests.Support.InvalidFixtureAssert;

namespace DialogueDown.Conformance;

public sealed class ReadableFixtureTests
{
    [Fact]
    public void Read_AFixture_CarriesWhatItClaims()
    {
        var fixture = ReadableFixture.Read("""
            {
              "name": "an unknown required capability is refused",
              "playbook": "playbook.json",
              "verdict": "refuse",
              "because": "requires 'detour', which no version-0 runtime offers"
            }
            """);

        Assert.Equal("an unknown required capability is refused", fixture.Name);
        Assert.Equal("playbook.json", fixture.Playbook);
        Assert.Equal(Verdict.Refuse, fixture.Verdict);
        Assert.Equal("requires 'detour', which no version-0 runtime offers", fixture.Because);
    }

    [Fact]
    public void Read_AnAcceptingFixture_IsUnderstood()
    {
        var fixture = ReadableFixture.Read(With("verdict", "accept"));

        Assert.Equal(Verdict.Accept, fixture.Verdict);
    }

    [Fact]
    public void Read_AVerdictNobodyCanActOn_IsRefused()
    {
        AssertInvalid(() => ReadableFixture.Read(With("verdict", "maybe")), "verdict");
    }

    [Fact]
    public void Read_AVerdictInAnotherCase_IsRefusedAsThePlaybookFormatRefusesItsOwn()
    {
        // A playbook refuses "Italic" for "italic", so the corpus is just as strict.
        AssertInvalid(() => ReadableFixture.Read(With("verdict", "Refuse")));
    }

    [Fact]
    public void Read_AVerdictWrittenAsANumber_IsRefused()
    {
        AssertInvalid(() => ReadableFixture.Read(With("verdict", 1)));
    }

    [Fact]
    public void Read_AFixturePointingAtItsSchema_KeepsTheUrl()
    {
        var fixture = ReadableFixture.Read(
            With("$schema", "https://pengzhengyi.github.io/dialoguedown/schema/fixture-0.schema.json"));

        Assert.Equal("https://pengzhengyi.github.io/dialoguedown/schema/fixture-0.schema.json", fixture.Schema);
    }

    [Fact]
    public void Read_AFixtureWithoutASchema_IsStillRead()
    {
        Assert.Null(ReadableFixture.Read(Fixture().ToJsonString()).Schema);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("playbook")]
    [InlineData("verdict")]
    [InlineData("because")]
    public void Read_AFixtureMissingAField_SaysWhichIsMissing(string missing)
    {
        AssertInvalid(() => ReadableFixture.Read(Without(missing)), missing);
    }

    [Fact]
    public void Read_AMisspelledField_IsRefusedRatherThanIgnored()
    {
        AssertInvalid(() => ReadableFixture.Read(With("verdcit", "accept")), "verdcit");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[]")]
    public void Read_SomethingThatIsNotAFixture_SaysSo(string json)
    {
        AssertInvalid(() => ReadableFixture.Read(json));
    }

    /// <summary>A well-formed fixture, which each test changes in one field.</summary>
    private static JsonObject Fixture() => new()
    {
        ["name"] = "a fixture",
        ["playbook"] = "playbook.json",
        ["verdict"] = "refuse",
        ["because"] = "a reason a reviewer can weigh",
    };

    /// <summary>The well-formed fixture with one field set, whether or not the field belongs.</summary>
    private static string With(string field, JsonNode value)
    {
        var fixture = Fixture();
        fixture[field] = value;

        return fixture.ToJsonString();
    }

    /// <summary>The well-formed fixture with one field removed.</summary>
    private static string Without(string field)
    {
        var fixture = Fixture();

        // Removing a field the fixture lacks would leave the test checking a complete fixture.
        Assert.True(fixture.Remove(field), $"'{field}' is not a field of a fixture.");

        return fixture.ToJsonString();
    }
}
