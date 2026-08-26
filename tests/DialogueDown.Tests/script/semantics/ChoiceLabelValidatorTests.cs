using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class ChoiceLabelValidatorTests
{
    [Fact]
    public void Validate_AnArmThatSpeaks_ReportsNothing() =>
        Assert.Empty(Diagnostics(Choice(Line(Text("Go east.")))));

    [Fact]
    public void Validate_AnArmThatOnlyJumps_ReportsNothing() =>
        // The jump's own words name the arm, so a menu has something to show.
        Assert.Empty(Diagnostics(Choice(ControlLine(Jump("#east", Text("Go east"))))));

    [Fact]
    public void Validate_AnArmWithOnlyAnEffect_IsReported() =>
        AssertReported(
            Diagnostics(Choice(ControlLine(DefaultCommand("fade out")))),
            DiagnosticCatalog.OptionWithNothingToShow);

    [Fact]
    public void Validate_AnEmptyArm_IsReported() =>
        // A player is offered the same blank row either way, and the way out is the same too.
        AssertReported(Diagnostics(Choice()), DiagnosticCatalog.OptionWithNothingToShow);

    [Fact]
    public void Validate_AnUnnamedJump_IsReported() =>
        // "=> [](#east)" leaves the arm as wordless as an empty one.
        AssertReported(
            Diagnostics(Choice(ControlLine(Jump("#east")))),
            DiagnosticCatalog.OptionWithNothingToShow);

    [Fact]
    public void Validate_ABlankArm_IsAWarning() =>
        Assert.Equal(
            DiagnosticSeverity.Warning,
            AssertReported(
                Diagnostics(Choice()), DiagnosticCatalog.OptionWithNothingToShow).Severity);

    [Fact]
    public void Validate_ReportsAtTheOffendingArmsSpan()
    {
        var span = new SourceSpan(7, 3);

        Assert.Equal(span, AssertReported(Diagnostics(new Choice([], span)), DiagnosticCatalog.OptionWithNothingToShow).Span);
    }

    [Fact]
    public void Validate_ReportsEveryBlankArm() =>
        Assert.Equal(2, Diagnostics(Choice(), Choice(Line(Text("Named."))), Choice()).Count);

    // Runs the validator over the given arms and returns what it reported.
    private static IReadOnlyList<Diagnostic> Diagnostics(params Choice[] options)
    {
        var bag = new DiagnosticBag();
        ChoiceLabelValidator.Validate(options, bag);
        return bag.Diagnostics;
    }
}
