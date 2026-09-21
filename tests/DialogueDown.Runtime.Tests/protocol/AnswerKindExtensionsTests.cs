using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests.Protocol;

public sealed class AnswerKindExtensionsTests
{
    /// <summary>One answer of every kind the protocol defines.</summary>
    public static ImmutableArray<Answer> OneOfEveryAnswerKind() =>
        [new BooleanAnswer(true), new TextAnswer("Ada")];

    [Fact]
    public void Kind_OfAYesOrNo_IsBoolean() =>
        Assert.Equal(AnswerKind.Boolean, new BooleanAnswer(true).Kind());

    [Fact]
    public void Kind_OfWords_IsText() => Assert.Equal(AnswerKind.Text, new TextAnswer("Ada").Kind());

    [Fact]
    public void Kind_IsNamedForEveryAnswerTheProtocolDefines()
    {
        var samples = OneOfEveryAnswerKind();

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added to the protocol and left out here would go unnamed with nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<Answer>(samples);

        Assert.Equal(samples.Length, samples.Select(answer => answer.Kind()).Distinct().Count());
    }

    [Fact]
    public void Kind_RefusesAnAnswerItWasNeverTaught() =>
        Assert.Throws<NotSupportedException>(() => new UntaughtAnswer().Kind());

    [Fact]
    public void Kind_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Answer)null!).Kind());

    [Fact]
    public void Describe_EveryKind_IsWorded() =>
        // A kind added to the protocol arrives here as a failure rather than as a refusal that
        // explains itself with a blank where the kind should be.
        Assert.All(
            Enum.GetValues<AnswerKind>(),
            kind => Assert.False(string.IsNullOrWhiteSpace(kind.Describe())));

    [Fact]
    public void Describe_ReadsAsPartOfASentence()
    {
        Assert.Equal("a truth", AnswerKind.Boolean.Describe());
        Assert.Equal("words", AnswerKind.Text.Describe());
    }

    [Fact]
    public void Describe_RefusesAKindItWasNeverTaught() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((AnswerKind)999).Describe());

    /// <summary>An answer kind nothing has been taught to tell apart, for the refusal case.</summary>
    private sealed record UntaughtAnswer : Answer;
}
