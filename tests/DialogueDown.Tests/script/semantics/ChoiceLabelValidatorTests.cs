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
    public void Validate_AnOptionThatSpeaks_ReportsNothing() =>
        Assert.Empty(Diagnostics(Choice(Line(Text("Go east.")))));

    [Fact]
    public void Validate_AnOptionThatOnlyJumps_ReportsNothing() =>
        // The jump's own words name the option, so a menu has something to show.
        Assert.Empty(Diagnostics(Choice(ControlLine(Jump("#east", Text("Go east"))))));

    [Fact]
    public void Validate_AnOptionWithOnlyAnEffect_IsReported() =>
        AssertReported(
            Diagnostics(Choice(ControlLine(DefaultCommand("fade out")))),
            DiagnosticCatalog.OptionWithNothingToShow);

    [Fact]
    public void Validate_AnEmptyOption_IsReported() =>
        // A player is offered the same blank row either way, and the way out is the same too.
        AssertReported(Diagnostics(Choice()), DiagnosticCatalog.OptionWithNothingToShow);

    [Fact]
    public void Validate_AnUnnamedJump_IsReported() =>
        // "=> [](#east)" leaves the option as wordless as an empty one.
        AssertReported(
            Diagnostics(Choice(ControlLine(Jump("#east")))),
            DiagnosticCatalog.OptionWithNothingToShow);

    [Fact]
    public void Validate_ABlankOption_IsAWarning() =>
        Assert.Equal(
            DiagnosticSeverity.Warning,
            AssertReported(
                Diagnostics(Choice()), DiagnosticCatalog.OptionWithNothingToShow).Severity);

    [Fact]
    public void Validate_ReportsAtTheOffendingOptionsSpan()
    {
        var span = new SourceSpan(7, 3);

        Assert.Equal(span, AssertReported(Diagnostics(Choice() with { Span = span }), DiagnosticCatalog.OptionWithNothingToShow).Span);
    }

    [Fact]
    public void Validate_ReportsEveryBlankOption() =>
        Assert.Equal(2, Diagnostics(Choice(), Choice(Line(Text("Named."))), Choice()).Count);

    // Runs the validator over the given options and returns what it reported.
    private static IReadOnlyList<Diagnostic> Diagnostics(params Choice[] options)
    {
        var bag = new DiagnosticBag();
        ChoiceLabelValidator.Validate(options, bag);
        return bag.Diagnostics;
    }
}
