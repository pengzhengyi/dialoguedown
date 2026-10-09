using static DialogueDown.Conformance.Tests.Support.FixtureJsonFactory;
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
        var fixture = ReadableFixture.Read(AReadableFixture().WithField("verdict", "accept"));

        Assert.Equal(Verdict.Accept, fixture.Verdict);
    }

    [Fact]
    public void Read_AVerdictNobodyCanActOn_IsRefused()
    {
        AssertInvalid(() => ReadableFixture.Read(AReadableFixture().WithField("verdict", "maybe")), "verdict");
    }

    [Fact]
    public void Read_AVerdictInAnotherCase_IsRefusedAsThePlaybookFormatRefusesItsOwn()
    {
        // A playbook refuses "Italic" for "italic", so the corpus is just as strict.
        AssertInvalid(() => ReadableFixture.Read(AReadableFixture().WithField("verdict", "Refuse")));
    }

    [Fact]
    public void Read_AVerdictWrittenAsANumber_IsRefused()
    {
        AssertInvalid(() => ReadableFixture.Read(AReadableFixture().WithField("verdict", 1)));
    }

    [Fact]
    public void Read_AFixturePointingAtItsSchema_KeepsTheUrl()
    {
        var fixture = ReadableFixture.Read(
            AReadableFixture().WithField("$schema", SchemaUrl));

        Assert.Equal(SchemaUrl, fixture.Schema);
    }

    [Fact]
    public void Read_AFixtureWithoutASchema_IsStillRead()
    {
        var fixture = ReadableFixture.Read(AReadableFixture().ToJsonString());

        Assert.Null(fixture.Schema);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("playbook")]
    [InlineData("verdict")]
    [InlineData("because")]
    public void Read_AFixtureMissingAField_SaysWhichIsMissing(string missing)
    {
        AssertInvalid(() => ReadableFixture.Read(AReadableFixture().WithoutField(missing)), missing);
    }

    [Fact]
    public void Read_AMisspelledField_IsRefusedRatherThanIgnored()
    {
        AssertInvalid(() => ReadableFixture.Read(AReadableFixture().WithField("verdcit", "accept")), "verdcit");
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
}
