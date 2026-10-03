using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Conformance;

/// <summary>
/// A playable case's playbook must be exactly what its source compiles to.
/// </summary>
/// <remarks>
/// A committed playbook that no longer matches its source asserts an outdated format. Only playable
/// cases are checked this way: a readable case's playbook is deliberately that compile with one
/// field broken.
/// </remarks>
public sealed class PlayableCaseTests
{
    private const string SourceFile = "source.dialogue.md";
    private const string PlaybookFile = "playbook.json";

    private static string Half => Path.Combine(AppContext.BaseDirectory, "conformance", "playable");

    public static TheoryData<string> Cases() =>
        [.. Directory.EnumerateDirectories(Half)
            .Select(folder => Path.GetFileName(folder)!)
            .Order(StringComparer.Ordinal)];

    [Fact]
    public void ThePlayableHalf_HasCases()
    {
        // Without this, emptying the half would leave the theory below passing on nothing.
        Assert.NotEmpty(Cases());
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ACase_ShipsThePlaybookItsSourceCompilesTo(string caseName)
    {
        var folder = Path.Combine(Half, caseName);

        var recompiled = Playbooks.Serialize(
            Playbooks.Of(File.ReadAllText(Path.Combine(folder, SourceFile)), SourceFile));

        Assert.Equal(File.ReadAllText(Path.Combine(folder, PlaybookFile)), recompiled);
    }
}
