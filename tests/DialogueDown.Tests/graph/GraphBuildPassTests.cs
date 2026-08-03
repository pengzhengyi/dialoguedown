using DialogueDown.Graph;
using static DialogueDown.Tests.Support.GraphBuildContextFactory;
using static DialogueDown.Tests.Support.GraphDraftFactory;

namespace DialogueDown.Tests.Graph;

public sealed class GraphBuildPassTests
{
    private readonly RecordingPass _pass = new();

    [Fact]
    public void Apply_NullDraft_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _pass.Apply(null!, Context("")));

    [Fact]
    public void Apply_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _pass.Apply(Draft(), null!));

    [Fact]
    public void Apply_ValidInputs_ForwardsThemToTheConcretePass()
    {
        var draft = Draft();
        var context = Context("Alice: a");

        _pass.Apply(draft, context);

        Assert.Same(draft, _pass.ReceivedDraft);
        Assert.Same(context, _pass.ReceivedContext);
    }

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
