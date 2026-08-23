using System.Text.Json.Nodes;

namespace DialogueDown.Playbook.Tests.Conformance;

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
        var error = Assert.Throws<InvalidFixtureException>(() => ReadableFixture.Read(With("verdict", "maybe")));

        Assert.Contains("verdict", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_AVerdictInAnotherCase_IsRefusedAsThePlaybookFormatRefusesItsOwn()
    {
        // A playbook refuses "Italic" for "italic", so the corpus documenting that format is
        // neither stricter nor looser than the format itself. One spelling, everywhere.
        Assert.Throws<InvalidFixtureException>(() => ReadableFixture.Read(With("verdict", "Refuse")));
    }

    [Fact]
    public void Read_AVerdictWrittenAsANumber_IsRefused()
    {
        Assert.Throws<InvalidFixtureException>(() => ReadableFixture.Read(With("verdict", 1)));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("playbook")]
    [InlineData("verdict")]
    [InlineData("because")]
    public void Read_AFixtureMissingAField_SaysWhichIsMissing(string missing)
    {
        var error = Assert.Throws<InvalidFixtureException>(() => ReadableFixture.Read(Without(missing)));

        Assert.Contains(missing, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_AMisspelledField_IsRefusedRatherThanIgnored()
    {
        // A fixture is hand-authored, so a typo is a mistake to surface -- the opposite of a
        // playbook, where an unknown property is a newer compiler talking to an older reader.
        var error = Assert.Throws<InvalidFixtureException>(() => ReadableFixture.Read(With("verdcit", "accept")));

        Assert.Contains("verdcit", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[]")]
    public void Read_SomethingThatIsNotAFixture_SaysSo(string json)
    {
        Assert.Throws<InvalidFixtureException>(() => ReadableFixture.Read(json));
    }

    /// <summary>A fixture whose every field is well formed, for one test to bend one way.</summary>
    private static JsonObject Fixture() => new()
    {
        ["name"] = "a fixture",
        ["playbook"] = "playbook.json",
        ["verdict"] = "refuse",
        ["because"] = "a reason a reviewer can weigh",
    };

    /// <summary>That fixture with one field set, whether or not it belongs there.</summary>
    private static string With(string field, JsonNode value)
    {
        var fixture = Fixture();
        fixture[field] = value;

        return fixture.ToJsonString();
    }

    /// <summary>That fixture with one field taken away.</summary>
    private static string Without(string field)
    {
        var fixture = Fixture();

        // A helper that silently removed nothing would leave the test asserting the opposite of
        // what it reads as.
        Assert.True(fixture.Remove(field), $"'{field}' is not a field of a fixture.");

        return fixture.ToJsonString();
    }
}
