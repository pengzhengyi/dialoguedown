using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Script;
using static DialogueDown.Visualization.Tests.Support.CompileAssert;

namespace DialogueDown.Visualization.Tests.Script;

public sealed class DialogueAstProjectionTests
{
    // A random choice with one option of each weight: a number weight, an auto weight, and a key
    // weight.
    private const string Weighted = """
        - `50%` Alice: Hi.
        - `%` Alice: Bye.
        - `"Bob.Affection"%` Alice: Later.
        """;

    // A control block with a conditional branch and an else branch.
    private const string IfElse = """
        > `if` `Rich?`
        >
        > Alice: Buy two.
        >
        > `else`
        >
        > Alice: Maybe later.
        """;

    // A line Alice speaks, then one the default speaker fills in at a zero-width position.
    private const string FilledDefault = """
        Alice: Hi.

        Bye.
        """;

    private readonly DialogueAstProjection _projection = new("Hi there");

    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DialogueAstProjection(null!));
    }

    [Fact]
    public void Title_IsDialogueAst()
    {
        Assert.Equal("Dialogue AST", _projection.Title);
    }

    [Fact]
    public void Description_IsANonEmptyOneLiner()
    {
        Assert.False(string.IsNullOrWhiteSpace(_projection.Description));
        Assert.DoesNotContain('\n', _projection.Description);
    }

    [Fact]
    public void Describe_Document_LabelsDocumentWithTheWholeSource()
    {
        var description = DescribeSingle<ScriptDocument>("Hi there");

        Assert.Equal("Document", description.Label);
        Assert.Equal("document", description.Category);
        Assert.Equal("Hi there", description.Source);
    }

    [Fact]
    public void Describe_UnsupportedNode_Throws()
    {
        Assert.Throws<ArgumentException>(() => _projection.Describe("not a node"));
    }

    [Fact]
    public void Describe_SlicesTheSourceFromTheSpan()
    {
        var description = DescribeSingle<Text>("Alice: Hi there.");

        Assert.Equal("Text", description.Label);
        Assert.Equal("text", description.Category);
        Assert.Equal("Hi there.", description.Source);
    }

    [Fact]
    public void Describe_Tag_UsesTagCategoryAndOmitsAnAbsentValue()
    {
        // Tags in a speaker prefix: a custom tag with a value, and a reserved tag without one.
        const string Source = "Alice #mood=happy ##default: Hi.";

        var withValue = DescribeSingle<CustomTag>(Source);
        Assert.Equal("Tag (custom)", withValue.Label);
        Assert.Equal("tag", withValue.Category);
        Assert.Equal(
            [new("name", "mood"), new("value", "happy"), new("span", "[6, 17)")],
            withValue.Attributes);

        var withoutValue = DescribeSingle<ReservedTag>(Source);
        Assert.Equal("Tag (reserved)", withoutValue.Label);
        Assert.Equal("tag", withoutValue.Category);
        Assert.Equal([new("name", "default"), new("span", "[18, 27)")], withoutValue.Attributes);
    }

    [Fact]
    public void Describe_SpeakerDeclaration_IncludesIdOnlyWhenPresent()
    {
        var withId = DescribeSingle<SpeakerDeclaration>("Alice @alice: Hi.");
        Assert.Equal("Speaker (declaration)", withId.Label);
        Assert.Equal("speech", withId.Category);
        Assert.Equal(
            [new("name", "Alice"), new("id", "alice"), new("span", "[0, 14)")],
            withId.Attributes);

        // A tag is what makes a prefix with no id a declaration. As with an id, the span covers
        // the whole prefix: "Alice #mood=happy: ", through the colon and the space after it.
        var withoutId = DescribeSingle<SpeakerDeclaration>("Alice #mood=happy: Hi.");
        Assert.Equal([new("name", "Alice"), new("span", "[0, 19)")], withoutId.Attributes);
    }

    [Fact]
    public void Describe_RandomChoices_LabelsItWithTheChoiceCategory()
    {
        var description = DescribeSingle<RandomChoices>(Weighted);

        Assert.Equal("Random choices", description.Label);
        Assert.Equal("choice", description.Category);
    }

    [Fact]
    public void Describe_RandomOption_ShowsItsWeightAsWritten()
    {
        Assert.Collection(
            DescribeAll<RandomOption>(Weighted),
            numeric =>
            {
                Assert.Equal("Random option", numeric.Label);
                Assert.Equal("choice", numeric.Category);
                Assert.Equal([new("weight", "50%"), new("span", "[0, 18)")], numeric.Attributes);
            },
            auto => Assert.Equal([new("weight", "%"), new("span", "[19, 36)")], auto.Attributes),
            query => Assert.Equal(
                [new("weight", "\"Bob.Affection\"%"), new("span", "[37, 71)")],
                query.Attributes));
    }

    [Fact]
    public void Neighbors_RandomChoices_YieldsOptions()
    {
        var random = AssertCompilesSingle<RandomChoices>(Weighted);

        AssertNeighbors(random, [.. random.Options]);
    }

    [Fact]
    public void Describe_Condition_ShowsItsKey_InTheCallCategory()
    {
        var description = DescribeSingle<Condition>("`Rainy?` Alice: Hi.");

        Assert.Equal("Condition", description.Label);
        Assert.Equal("call", description.Category);
        Assert.Equal([new("key", "Rainy"), new("span", "[0, 8)")], description.Attributes);
    }

    [Fact]
    public void Neighbors_ConditionalJump_YieldsTheConditionThenTheLabel()
    {
        var jump = AssertDesugarsSingle<Jump>("`Rainy?` => [The end](#END)");

        AssertNeighbors(jump, [jump.Condition, .. jump.Label]);
    }

    [Fact]
    public void Neighbors_UnconditionalJump_YieldsOnlyTheLabel()
    {
        var jump = AssertDesugarsSingle<Jump>("=> [The end](#END)");

        AssertNeighbors(jump, [.. jump.Label]);
    }

    [Fact]
    public void Neighbors_RandomOption_YieldsBody()
    {
        var option = AssertCompiles<RandomOption>(Weighted)[0];

        AssertNeighbors(option, [.. option.Body]);
    }

    [Fact]
    public void Neighbors_ConditionalChoice_YieldsTheConditionThenBody()
    {
        const string Source = """
            - `Rainy?` Alice: Hi.
            - Alice: Bye.
            """;
        var choice = AssertCompiles<Choice>(Source)[0];

        AssertNeighbors(choice, [choice.Condition, .. choice.Body]);
    }

    [Fact]
    public void Neighbors_ConditionalRandomOption_YieldsTheConditionThenBody()
    {
        const string Source = """
            - `50%` Alice: Hi.
            - `Rainy?` `50%` Alice: Bye.
            """;
        var option = AssertCompiles<RandomOption>(Source)[1];

        AssertNeighbors(option, [option.Condition, .. option.Body]);
    }

    [Fact]
    public void Describe_ControlBlock_LabelsItWithTheControlCategory()
    {
        var description = DescribeSingle<ControlBlock>(IfElse);

        Assert.Equal("Control block", description.Label);
        Assert.Equal("control", description.Category);
    }

    [Fact]
    public void Describe_Branch_LabelsAConditionalBranchWithTheControlCategory()
    {
        var description = DescribeAll<Branch>(IfElse)[0];

        Assert.Equal("Branch", description.Label);
        Assert.Equal("control", description.Category);
    }

    [Fact]
    public void Describe_ElseBranch_LabelsItAsAnElseBranch()
    {
        var description = DescribeAll<Branch>(IfElse)[1];

        Assert.Equal("Else branch", description.Label);
        Assert.Equal("control", description.Category);
    }

    [Fact]
    public void Neighbors_ControlBlock_YieldsBranches()
    {
        var control = AssertCompilesSingle<ControlBlock>(IfElse);

        AssertNeighbors(control, [.. control.Branches]);
    }

    [Fact]
    public void Neighbors_Branch_YieldsTheConditionThenBody()
    {
        var branch = AssertCompiles<Branch>(IfElse)[0];

        AssertNeighbors(branch, [branch.Condition, .. branch.Body]);
    }

    [Fact]
    public void Neighbors_ElseBranch_YieldsOnlyBody()
    {
        var branch = AssertCompiles<Branch>(IfElse)[1];

        AssertNeighbors(branch, [.. branch.Body]);
    }

    [Fact]
    public void Neighbors_Line_YieldsSpeakerThenSpeech()
    {
        var line = AssertCompilesSingle<Line>("Alice: Hi.");

        AssertNeighbors(line, [line.Speaker, .. line.Speech]);
    }

    [Fact]
    public void Neighbors_LineWithoutSpeaker_YieldsOnlySpeech()
    {
        // The transpiled line names no speaker; desugaring fills the default in later.
        var line = AssertCompilesSingle<Line>("Hi there");

        AssertNeighbors(line, [.. line.Speech]);
    }

    [Fact]
    public void Neighbors_ConditionalLine_YieldsTheConditionThenSpeakerThenSpeech()
    {
        var line = AssertCompilesSingle<Line>("`Rainy?` Alice: Hi.");

        AssertNeighbors(line, [line.Condition, line.Speaker, .. line.Speech]);
    }

    [Fact]
    public void Neighbors_Leaf_IsEmpty()
    {
        Assert.Empty(_projection.Neighbors(AssertCompilesSingle<Text>("Hi there")));
    }

    [Fact]
    public void Describe_DefaultSpeaker_LabelsItDefaultWithSpeechCategory()
    {
        var description = DescribeDesugaredSingle<DefaultSpeaker>(FilledDefault);

        Assert.Equal("Speaker (default)", description.Label);
        Assert.Equal("speech", description.Category);
        Assert.Equal([new("span", "[12, 12)")], description.Attributes);
    }

    [Fact]
    public void Describe_EmptySpanNode_HasNoSource()
    {
        // A synthetic node (a filled default speaker) marks a zero-width position, so it
        // has no source to slice — null, not an empty string that renders as a blank block.
        var description = DescribeDesugaredSingle<DefaultSpeaker>(FilledDefault);

        Assert.Null(description.Source);
    }

    [Fact]
    public void Describe_JumpIndicator_KeepsItsOwnLabelDistinctFromAnAssembledJump()
    {
        // The pre-desugar `=>` marker and the assembled Jump are different nodes; the
        // indicator keeps its own name so it stands out from the Jump it becomes.
        var indicator = DescribeSingle<JumpIndicator>("=> [The end](#END)");

        Assert.Equal("Jump indicator", indicator.Label);
        Assert.Equal("jump", indicator.Category);
    }

    [Fact]
    public void Describe_Jump_LabelsItWithTargetAndLabel()
    {
        var jump = DescribeDesugaredSingle<Jump>("=> [The end](#END)");

        Assert.Equal("Jump", jump.Label);
        Assert.Equal("jump", jump.Category);
        Assert.Equal(
            [new("target", "#END"), new("label", "The end"), new("span", "[0, 18)")],
            jump.Attributes);
    }

    [Fact]
    public void Neighbors_Jump_YieldsItsLabelFragments()
    {
        var jump = AssertDesugarsSingle<Jump>("=> [The **end**](#END)");

        AssertNeighbors(jump, [.. jump.Label]);
    }

    [Fact]
    public void Constructor_UsesGivenTitleAndDescription()
    {
        var projection = new DialogueAstProjection("Hi there", "Desugared AST", "the normalized tree");

        Assert.Equal("Desugared AST", projection.Title);
        Assert.Equal("the normalized tree", projection.Description);
    }

    [Fact]
    public void Describe_SpannedNode_CarriesTheStructuredSpan()
    {
        var description = DescribeSingle<Text>("Alice: Hi there.");

        Assert.Equal(new DisplaySpan(7, 16), description.Span);
    }

    [Fact]
    public void Describe_Document_SpansTheWholeSource()
    {
        var description = DescribeSingle<ScriptDocument>("Hi there");

        Assert.Equal(new DisplaySpan(0, "Hi there".Length), description.Span);
    }

    [Fact]
    public void Describe_SyntheticNode_CarriesItsZeroWidthCaretPosition()
    {
        // A filled default speaker carries an empty (zero-width) span: a caret position, not a
        // range of source. It has no source text to slice, but it keeps its position so a client
        // can place the cursor there — a "jump to source" on a synthetic node.
        var description = DescribeDesugaredSingle<DefaultSpeaker>(FilledDefault);

        Assert.Equal(new DisplaySpan(12, 12), description.Span);
    }

    // The projection clamps a span that runs past the end of its source rather than throw. No
    // script in these tests produces one, so the node is built by hand.
    [Fact]
    public void Describe_SpanPastEnd_ClampsToTheSource()
    {
        var description = _projection.Describe(new Text("x", new SourceSpan(5, 20)));

        Assert.Equal(new DisplaySpan(5, "Hi there".Length), description.Span);
    }

    // Describes the one node of the type given that the source compiles to, reading its snippet
    // and span from that source.
    private static NodeDescription DescribeSingle<TNode>(string source)
        where TNode : notnull =>
        new DialogueAstProjection(source).Describe(AssertCompilesSingle<TNode>(source));

    // Describes every node of the type given that the source compiles to, in document order.
    private static IReadOnlyList<NodeDescription> DescribeAll<TNode>(string source)
        where TNode : notnull
    {
        var projection = new DialogueAstProjection(source);
        return [.. AssertCompiles<TNode>(source).Select(node => projection.Describe(node))];
    }

    // Describes the one node of the type given in the source's desugared tree.
    private static NodeDescription DescribeDesugaredSingle<TNode>(string source)
        where TNode : notnull =>
        new DialogueAstProjection(source).Describe(AssertDesugarsSingle<TNode>(source));

    // Asserts the projection yields exactly the children given, in order. The children are read
    // from the node itself, so a node with none would pass without checking anything; the guard
    // keeps every fixture meaningful.
    private void AssertNeighbors(object node, IReadOnlyList<object?> expected)
    {
        Assert.NotEmpty(expected);
        Assert.Equal(expected, _projection.Neighbors(node));
    }
}
