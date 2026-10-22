using System.Collections.Immutable;
using CsCheck;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Checks that the generator behind the walk property produces every node and edge kind the
/// runner can play.
/// </summary>
/// <remarks>
/// The generator produces only the kinds the runner can play. Every kind it leaves out is listed
/// with a note on what must change first, and these tests fail once the runner plays a listed kind,
/// so the generator keeps up with the runner.
/// </remarks>
public sealed class PlaybookGenTests
{
    // Node and edge kinds the generator leaves out, each with a note on what must change first.
    // The test below checks the note on every node kind. An edge kind stays out for as long as the
    // node kind it belongs to does, because that node is the only place it can appear.
    private static readonly Dictionary<string, string> _notGenerated = new(StringComparer.Ordinal);

    [Fact]
    public void EveryKindTheFormatDefines_IsGeneratedOrLeftOutOnPurpose()
    {
        var generated = GeneratedKinds();
        var unaccounted = UnionMembers.NamesOf<Node>()
            .Concat(UnionMembers.NamesOf<Edge>())
            .Where(kind => !generated.Contains(kind) && !_notGenerated.ContainsKey(kind))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unaccounted.Count == 0,
            $"{unaccounted.Count} kind(s) the format defines are neither generated nor left out on "
                + $"purpose: {string.Join(", ", unaccounted)}. Generate each one, so the walk "
                + "property covers it, or list it with a note on what must change first.");
    }

    [Fact]
    public void EveryNodeKindLeftOut_IsStillOneTheRunnerCannotPlay()
    {
        // Fails once the runner plays a listed kind, naming the kind the generator must now
        // produce.
        foreach (var node in PlayContextFactory.OneOfEveryNodeKind())
        {
            var kind = node.GetType().Name;

            if (_notGenerated.TryGetValue(kind, out var reason))
            {
                Assert.True(
                    node.IsUnplayableKind(),
                    $"{kind} is left out of the generator because {reason}, and the runner plays "
                        + "one now. Generate it, so the walk property covers what the runner "
                        + "learned, and remove its entry from the list.");
            }
        }
    }

    [Fact]
    public void TheSamples_ReachMoreThanAHandfulOfKinds()
    {
        // Checks that GeneratedKinds still sees what the generator produced. If it saw nothing, the
        // check above would find nothing missing and pass.
        Assert.True(GeneratedKinds().Count >= 5);
    }

    [Fact]
    public void TheGenerator_PutsACommandEverywhereALineCanHoldOne()
    {
        // A line reaches the host in parts around each command, so the walk covers every part only
        // if some sampled line holds its command first, between its words, last, and alone.
        GenCoverageAssert.AssertCoversEvery(
            PlaybookGen.Valid(),
            ["alone", "between", "first", "last"],
            (playbook, place) => playbook.Nodes.OfType<LineNode>()
                .Any(line => WhereTheCommandStands(line.Speech) == place));
    }

    [Fact]
    public void TheGenerator_GivesSomeRandomChoiceNothingDrawable_InAWorldSampledBesideIt()
    {
        // A random choice is walked past only when no random option has a share, so the walk covers
        // that only if some sampled random choice has nothing drawable in a world the walk can
        // meet.
        var covered = GenCoverage.Covers(
            Gen.Select(PlaybookGen.Valid(), PlaybookGen.Worlds(), LeftNothingDrawable),
            randomChoices => randomChoices.Any());

        Assert.True(covered, "No sampled random choice had nothing drawable.");
    }

    [Fact]
    public void TheGenerator_GivesARandomChoiceAFallThrough_WheneverAWorldCanLeaveItNothingDrawable()
    {
        // A script compiles a random choice with a fall-through whenever the game can leave it
        // nothing drawable, so a walk over sampled playbooks is never refused there as leading
        // nowhere.
        Gen.Select(PlaybookGen.Valid(), PlaybookGen.Worlds()).Sample(
            (playbook, world) =>
            {
                foreach (var random in LeftNothingDrawable(playbook, world))
                {
                    Assert.True(
                        random.SuccessionTarget() is not null,
                        $"Random choice {random.Id} has nothing drawable in a sampled world, and no fall-through.");
                }
            });
    }

    /// <summary>
    /// Every sampled playbook is one the reader accepts.
    /// </summary>
    /// <remarks>
    /// The runner walk properties are about playbooks the reader accepts, so this asks the reader
    /// itself rather than trusting the generator to follow its rules.
    /// </remarks>
    [Fact]
    public void EverySampledPlaybook_IsOneTheReaderAccepts() =>
        PlaybookGen.Valid().Sample(PlaybookAssert.AssertValidPlaybook);

    /// <summary>
    /// The type name of every node and edge kind the fixed samples from the generator hold.
    /// </summary>
    /// <remarks>
    /// Read from the playbooks the generator actually produced, so the answer keeps up with the
    /// generator as it changes.
    /// </remarks>
    private static IReadOnlySet<string> GeneratedKinds() =>
        GenCoverage.Covered(
            PlaybookGen.Valid(),
            playbook => playbook.Nodes.SelectMany(
                node => node.Out.Select(edge => edge.GetType().Name).Prepend(node.GetType().Name)));

    /// <summary>
    /// Every random choice in a playbook that a sampled world leaves nothing drawable.
    /// </summary>
    /// <param name="playbook">The playbook.</param>
    /// <param name="world">The world sampled beside it.</param>
    /// <returns>The random choices none of whose random options has a share in that world.</returns>
    private static IEnumerable<RandomChoiceNode> LeftNothingDrawable(
        PlaybookDocument playbook, SampledWorld world) =>
        playbook.Nodes.OfType<RandomChoiceNode>()
            .Where(random => RandomChoiceShares.HasNothingDrawable(random, ArrivingAt(random, world)));

    /// <summary>What a sampled world answers when a run arrives at a random choice.</summary>
    /// <param name="random">The random choice.</param>
    /// <param name="world">The world sampled beside its playbook.</param>
    /// <returns>Each key its guards and key weights ask about, answered.</returns>
    private static Supply ArrivingAt(RandomChoiceNode random, SampledWorld world) =>
        world.Answering(NodeQuestions.RequiredToPlay(random).Keys());

    /// <summary>Where a line's speech holds its first command.</summary>
    /// <param name="speech">The line's speech.</param>
    /// <returns>
    /// <c>first</c>, <c>between</c>, <c>last</c>, or <c>alone</c>, or <see langword="null"/> when the
    /// speech holds no command.
    /// </returns>
    private static string? WhereTheCommandStands(ImmutableArray<SpeechFragment> speech)
    {
        var at = speech.ToList().FindIndex(fragment => fragment is CustomCommandFragment or DefaultCommandFragment);

        return (at, speech.Length) switch
        {
            (-1, _) => null,
            (0, 1) => "alone",
            (0, _) => "first",
            var (_, length) when at == length - 1 => "last",
            _ => "between",
        };
    }
}
