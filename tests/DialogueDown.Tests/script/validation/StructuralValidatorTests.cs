using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using NSubstitute;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.StructuralValidatorFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class StructuralValidatorTests
{
    [Fact]
    public void Validate_RunsEachRuleWithOneSharedIndexAndTheSink()
    {
        DialogueTreeIndex? seenByFirst = null;
        DialogueTreeIndex? seenBySecond = null;
        var first = Substitute.For<IDiagnosticRule>();
        var second = Substitute.For<IDiagnosticRule>();
        first.When(r => r.Check(Arg.Any<DialogueTreeIndex>(), Arg.Any<IDiagnosticSink>()))
            .Do(call => seenByFirst = call.Arg<DialogueTreeIndex>());
        second.When(r => r.Check(Arg.Any<DialogueTreeIndex>(), Arg.Any<IDiagnosticSink>()))
            .Do(call => seenBySecond = call.Arg<DialogueTreeIndex>());
        var sink = new DiagnosticBag();

        new StructuralValidator([first, second]).Validate(Document(), sink);

        first.Received(1).Check(Arg.Any<DialogueTreeIndex>(), sink);
        second.Received(1).Check(Arg.Any<DialogueTreeIndex>(), sink);
        Assert.NotNull(seenByFirst);
        Assert.Same(seenByFirst, seenBySecond); // one index, built once and shared
    }

    [Fact]
    public void Validate_WithTheUnreachableAfterJumpRule_ReportsForContentAfterAJump()
    {
        var document = Document(Line(Jump("#a"), Jump("#b")));
        var sink = new DiagnosticBag();

        new StructuralValidator([new UnreachableAfterJumpRule()]).Validate(document, sink);

        Assert.Equal(
            DiagnosticCatalog.UnreachableContentAfterJump.Code,
            Assert.Single(sink.Diagnostics).Descriptor.Code);
    }

    [Fact]
    public void Validate_NoRules_ReportsNothing()
    {
        var sink = new DiagnosticBag();

        WithoutRules().Validate(Document(), sink);

        Assert.Empty(sink.Diagnostics);
    }

    [Fact]
    public void Constructor_NullRules_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StructuralValidator(null!));
    }

    [Fact]
    public void Validate_NullDocument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WithoutRules().Validate(null!, new DiagnosticBag()));
    }

    [Fact]
    public void Validate_NullDiagnostics_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WithoutRules().Validate(Document(), null!));
    }

    private static DesugaredScriptDocument Document(params ScriptBlock[] blocks) =>
        new(new ScriptDocument(blocks));
}
