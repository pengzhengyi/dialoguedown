using DialogueDown.Graph;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.GraphDraftFactory;

namespace DialogueDown.Tests.Graph;

public sealed class GraphBuildPassTests
{
    private readonly RecordingPass _pass = new();

    [Fact]
    public void Apply_NullDraft_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _pass.Apply(null!, BuildContext("")));

    [Fact]
    public void Apply_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _pass.Apply(Draft(), null!));

    [Fact]
    public void Apply_ValidInputs_ForwardsThemToTheConcretePass()
    {
        var draft = Draft();
        var context = BuildContext("Alice: a");

        _pass.Apply(draft, context);

        Assert.Same(draft, _pass.ReceivedDraft);
        Assert.Same(context, _pass.ReceivedContext);
    }

    private static GraphBuildContext BuildContext(string source) =>
        new(Pipeline.UntilAnalyzed(source), DiagnosticsContextFactory.Context(source));

    private sealed class RecordingPass : GraphBuildPass
    {
        public GraphDraft? ReceivedDraft { get; private set; }

        public GraphBuildContext? ReceivedContext { get; private set; }

        protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
        {
            ReceivedDraft = draft;
            ReceivedContext = context;
        }
    }
}
