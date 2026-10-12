using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ControlLineTests
{
    // An effect is never attributed to a speaker, so a control line's type carries no speaker.
    [Fact]
    public void ControlLine_ExposesNoSpeaker() =>
        Assert.Null(typeof(ControlLine).GetProperty("Speaker"));

    [Fact]
    public void IsConditional_ReflectsWhetherAConditionGuardsTheControlLine()
    {
        var span = SourceSpanFactory.Span();
        var effects = new InlineFragment[] { DefaultCommand("open the gate") };

        Assert.False(new ControlLine(effects, span).IsConditional());
        Assert.True(new ControlLine(effects, span, Condition("GateJammed")).IsConditional());
    }
}
