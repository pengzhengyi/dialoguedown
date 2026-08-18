using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Visualization.Tests.Script;

public sealed class DialogueAstProjectionTests
{
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
        var description = _projection.Describe(new ScriptDocument([]));

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
        var description = _projection.Describe(new Text("Hi", new SourceSpan(0, 2)));

        Assert.Equal("Text", description.Label);
        Assert.Equal("text", description.Category);
        Assert.Equal("Hi", description.Source);
    }

    [Fact]
    public void Describe_Tag_UsesTagCategoryAndOmitsAnAbsentValue()
    {
        var withValue = _projection.Describe(new CustomTag("mood", "happy", new SourceSpan(0, 5)));
        Assert.Equal("Tag (custom)", withValue.Label);
        Assert.Equal("tag", withValue.Category);
        Assert.Contains(withValue.Attributes, a => a.Name == "value" && a.Value == "happy");

        var withoutValue = _projection.Describe(new ReservedTag("hidden", null, new SourceSpan(0, 5)));
        Assert.Equal("Tag (reserved)", withoutValue.Label);
        Assert.Equal("tag", withoutValue.Category);
        Assert.DoesNotContain(withoutValue.Attributes, a => a.Name == "value");
    }

    [Fact]
    public void Describe_SpeakerDeclaration_IncludesIdOnlyWhenPresent()
    {
        var span = new SourceSpan(0, 5);

        var withId = _projection.Describe(new SpeakerDeclaration("Alice", "alice", [], span));
        Assert.Equal("Speaker (declaration)", withId.Label);
        Assert.Equal("speech", withId.Category);
        Assert.Contains(withId.Attributes, a => a.Name == "id" && a.Value == "alice");

        var withoutId = _projection.Describe(new SpeakerDeclaration("Alice", null, [], span));
        Assert.DoesNotContain(withoutId.Attributes, a => a.Name == "id");
    }

    [Fact]
    public void Describe_RandomChoices_LabelsItWithTheChoiceCategory()
    {
        var description = _projection.Describe(new RandomChoices([], new SourceSpan(0, 5)));

        Assert.Equal("Random choices", description.Label);
        Assert.Equal("choice", description.Category);
    }

    [Fact]
    public void Describe_RandomOption_ShowsItsWeightAsWritten()
    {
        var span = new SourceSpan(0, 5);

        var numeric = _projection.Describe(new RandomOption(new NumberWeight(50, span), [], span));
        Assert.Equal("Random option", numeric.Label);
        Assert.Equal("choice", numeric.Category);
        Assert.Contains(numeric.Attributes, a => a.Name == "weight" && a.Value == "50%");

        var auto = _projection.Describe(new RandomOption(new AutoWeight(span), [], span));
        Assert.Contains(auto.Attributes, a => a.Name == "weight" && a.Value == "%");

        var query = _projection.Describe(
            new RandomOption(new QueryWeight("Bob.Affection", span), [], span));
        Assert.Contains(query.Attributes, a => a.Name == "weight" && a.Value == "\"Bob.Affection\"%");
    }

    [Fact]
    public void Neighbors_RandomChoices_YieldsOptions()
    {
        var option = new RandomOption(new AutoWeight(new SourceSpan(0, 1)), [], new SourceSpan(0, 1));
        var random = new RandomChoices([option], new SourceSpan(0, 5));

        Assert.Equal(new object[] { option }, _projection.Neighbors(random));
    }

    [Fact]
    public void Describe_Condition_ShowsItsKey_InTheCallCategory()
    {
        var description = _projection.Describe(new Condition("Rainy", new SourceSpan(0, 9)));

        Assert.Equal("Condition", description.Label);
        Assert.Equal("call", description.Category);
        Assert.Contains(description.Attributes, a => a.Name == "key" && a.Value == "Rainy");
    }

    [Fact]
    public void Neighbors_ConditionalJump_YieldsTheConditionThenTheLabel()
    {
        var span = new SourceSpan(0, 5);
        var condition = new Condition("Rainy", span);
        var label = new Text("go", span);
        var jump = new Jump("#x", [label], span, condition);

        Assert.Equal(new object[] { condition, label }, _projection.Neighbors(jump));
    }

    [Fact]
    public void Neighbors_UnconditionalJump_YieldsOnlyTheLabel()
    {
        var span = new SourceSpan(0, 5);
        var label = new Text("go", span);
        var jump = new Jump("#x", [label], span);

        Assert.Equal(new object[] { label }, _projection.Neighbors(jump));
    }

    [Fact]
    public void Neighbors_RandomOption_YieldsBody()
    {
        var line = new Line(null, [new Text("x", new SourceSpan(0, 1))], new SourceSpan(0, 1));
        var option = new RandomOption(new NumberWeight(50, new SourceSpan(0, 5)), [line], new SourceSpan(0, 5));

        Assert.Equal(new object[] { line }, _projection.Neighbors(option));
    }

    [Fact]
    public void Neighbors_ConditionalChoice_YieldsTheConditionThenBody()
    {
        var span = new SourceSpan(0, 5);
        var condition = new Condition("Rainy", span);
        var line = new Line(null, [new Text("x", span)], span);
        var choice = new Choice([line], span, condition);

        Assert.Equal(new object[] { condition, line }, _projection.Neighbors(choice));
    }

    [Fact]
    public void Neighbors_ConditionalRandomOption_YieldsTheConditionThenBody()
    {
        var span = new SourceSpan(0, 5);
        var condition = new Condition("Rainy", span);
        var line = new Line(null, [new Text("x", span)], span);
        var option = new RandomOption(new NumberWeight(50, span), [line], span, condition);

        Assert.Equal(new object[] { condition, line }, _projection.Neighbors(option));
    }

    [Fact]
    public void Describe_ControlBlock_LabelsItWithTheControlCategory()
    {
        var description = _projection.Describe(new ControlBlock([], new SourceSpan(0, 5)));

        Assert.Equal("Control block", description.Label);
        Assert.Equal("control", description.Category);
    }

    [Fact]
    public void Describe_Branch_LabelsAConditionalBranchWithTheControlCategory()
    {
        var span = new SourceSpan(0, 5);
        var branch = new Branch(new Condition("Rich", span), [], span);

        var description = _projection.Describe(branch);

        Assert.Equal("Branch", description.Label);
        Assert.Equal("control", description.Category);
    }

    [Fact]
    public void Describe_ElseBranch_LabelsItAsAnElseBranch()
    {
        var span = new SourceSpan(0, 5);

        var description = _projection.Describe(new Branch(null, [], span));

        Assert.Equal("Else branch", description.Label);
        Assert.Equal("control", description.Category);
    }

    [Fact]
    public void Neighbors_ControlBlock_YieldsBranches()
    {
        var span = new SourceSpan(0, 5);
        var branch = new Branch(new Condition("Rich", span), [], span);
        var control = new ControlBlock([branch], span);

        Assert.Equal(new object[] { branch }, _projection.Neighbors(control));
    }

    [Fact]
    public void Neighbors_Branch_YieldsTheConditionThenBody()
    {
        var span = new SourceSpan(0, 5);
        var condition = new Condition("Rich", span);
        var line = new Line(null, [new Text("x", span)], span);
        var branch = new Branch(condition, [line], span);

        Assert.Equal(new object[] { condition, line }, _projection.Neighbors(branch));
    }

    [Fact]
    public void Neighbors_ElseBranch_YieldsOnlyBody()
    {
        var span = new SourceSpan(0, 5);
        var line = new Line(null, [new Text("x", span)], span);
        var branch = new Branch(null, [line], span);

        Assert.Equal(new object[] { line }, _projection.Neighbors(branch));
    }

    [Fact]
    public void Neighbors_Line_YieldsSpeakerThenSpeech()
    {
        var span = new SourceSpan(0, 5);
        var speaker = new SpeakerNameReference("Alice", span);
        var text = new Text("Hi", span);
        var line = new Line(speaker, [text], span);

        Assert.Equal(new object[] { speaker, text }, _projection.Neighbors(line));
    }

    [Fact]
    public void Neighbors_LineWithoutSpeaker_YieldsOnlySpeech()
    {
        var span = new SourceSpan(0, 2);
        var text = new Text("Hi", span);

        Assert.Equal(new object[] { text }, _projection.Neighbors(new Line(null, [text], span)));
    }

    [Fact]
    public void Neighbors_ConditionalLine_YieldsTheConditionThenSpeakerThenSpeech()
    {
        var span = new SourceSpan(0, 5);
        var condition = new Condition("Rainy", span);
        var speaker = new SpeakerNameReference("Alice", span);
        var text = new Text("Hi", span);
        var line = new Line(speaker, [text], span, condition);

        Assert.Equal(new object[] { condition, speaker, text }, _projection.Neighbors(line));
    }

    [Fact]
    public void Neighbors_Leaf_IsEmpty()
    {
        Assert.Empty(_projection.Neighbors(new Text("Hi", new SourceSpan(0, 2))));
    }

    [Fact]
    public void Describe_DefaultSpeaker_LabelsItDefaultWithSpeechCategory()
    {
        var description = _projection.Describe(new DefaultSpeaker(SourceSpan.EmptyAt(3)));

        Assert.Equal("Speaker (default)", description.Label);
        Assert.Equal("speech", description.Category);
        Assert.Contains(description.Attributes, a => a.Name == "span");
    }

    [Fact]
    public void Describe_EmptySpanNode_HasNoSource()
    {
        // A synthetic node (a filled default speaker) marks a zero-width position, so it
        // has no source to slice — null, not an empty string that renders as a blank block.
        var description = _projection.Describe(new DefaultSpeaker(SourceSpan.EmptyAt(3)));

        Assert.Null(description.Source);
    }

    [Fact]
    public void Describe_JumpIndicator_KeepsItsOwnLabelDistinctFromAnAssembledJump()
    {
        // The pre-desugar `=>` marker and the assembled Jump are different nodes; the
        // indicator keeps its own name so it stands out from the Jump it becomes.
        var indicator = _projection.Describe(new JumpIndicator(new SourceSpan(0, 2)));

        Assert.Equal("Jump indicator", indicator.Label);
        Assert.Equal("jump", indicator.Category);
    }

    [Fact]
    public void Describe_Jump_LabelsItWithTargetAndLabel()
    {
        var span = new SourceSpan(0, 8);
        var jump = _projection.Describe(new Jump("scene-2", [new Text("Go", span)], span));

        Assert.Equal("Jump", jump.Label);
        Assert.Equal("jump", jump.Category);
        Assert.Contains(jump.Attributes, a => a.Name == "target" && a.Value == "scene-2");
        Assert.Contains(jump.Attributes, a => a.Name == "label" && a.Value == "Go");
    }

    [Fact]
    public void Neighbors_Jump_YieldsItsLabelFragments()
    {
        var span = new SourceSpan(0, 2);
        var label = new Text("Go", span);

        Assert.Equal(new object[] { label }, _projection.Neighbors(new Jump("t", [label], span)));
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
        var description = _projection.Describe(new Text("Hi", new SourceSpan(0, 2)));

        Assert.Equal(new DisplaySpan(0, 2), description.Span);
    }

    [Fact]
    public void Describe_Document_SpansTheWholeSource()
    {
        var description = _projection.Describe(new ScriptDocument([]));

        Assert.Equal(new DisplaySpan(0, "Hi there".Length), description.Span);
    }

    [Fact]
    public void Describe_SyntheticNode_CarriesItsZeroWidthCaretPosition()
    {
        // A filled default speaker carries an empty (zero-width) span: a caret position, not a
        // range of source. It has no source text to slice, but it keeps its position so a client
        // can place the cursor there — a "jump to source" on a synthetic node.
        var description = _projection.Describe(new DefaultSpeaker(new SourceSpan(3, 0)));

        Assert.Equal(new DisplaySpan(3, 3), description.Span);
    }

    [Fact]
    public void Describe_SpanPastEnd_ClampsToTheSource()
    {
        var description = _projection.Describe(new Text("x", new SourceSpan(5, 20)));

        Assert.Equal(new DisplaySpan(5, "Hi there".Length), description.Span);
    }
}
