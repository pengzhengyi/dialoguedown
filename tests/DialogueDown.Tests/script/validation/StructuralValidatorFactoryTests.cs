using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class StructuralValidatorFactoryTests
{
    [Fact]
    public void CreateDefault_ReportsACommandInALabel()
    {
        var diagnostics = new DiagnosticBag();

        StructuralValidatorFactory.CreateDefault().Validate(ACommandInALinkLabel(), diagnostics);

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.CommandInLabel);
    }

    /// <summary>A script whose only line holds a command in a link label.</summary>
    /// <remarks>
    /// <code>
    /// Alice: [Talk to `Wave()`](#inn)
    /// </code>
    /// </remarks>
    private static DesugaredScriptDocument ACommandInALinkLabel() =>
        new(Document(Line(Link("#inn", Text("Talk to "), CustomCommand("Wave")))));
}
