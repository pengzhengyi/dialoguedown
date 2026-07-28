using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Ast;

public sealed class ControlLineTests
{
    // The whole point of a control line: an effect is never attributed to a speaker, so the type
    // must not carry one. This guards against a speaker sneaking back on in a later change.
    [Fact]
    public void ControlLine_ExposesNoSpeaker() =>
        Assert.Null(typeof(ControlLine).GetProperty("Speaker"));

    [Fact]
    public void IsConditional_ReflectsWhetherAConditionGuardsTheControlLine()
    {
        var span = SourceSpanFactory.Span();
        var effects = new InlineFragment[] { new DefaultCommand("open the gate", span) };

        Assert.False(new ControlLine(effects, span).IsConditional());
        Assert.True(new ControlLine(effects, span, new Condition("GateJammed", span)).IsConditional());
    }
}
