using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class JumpResolverTests
{
    [Fact]
    public void Resolve_SameFileAnchor_ResolvesToItsScene()
    {
        var anchors = AnchorsFor(SceneHeading("Play tennis", 1));

        var resolution = ResolveOne(Jump("#play-tennis"), anchors);

        var sceneJump = Assert.IsType<SceneJump>(resolution);
        Assert.Equal("play-tennis", sceneJump.Scene.Anchor);
    }

    [Fact]
    public void Resolve_SameFileAnchor_MissingScene_ReportsAndLeavesItUnresolved()
    {
        var anchors = AnchorsFor(SceneHeading("Play tennis", 1));

        var resolution = ResolveOne(Jump("#no-such-scene"), anchors, out var diagnostics);

        Assert.IsType<UnresolvedJump>(resolution);
        var diagnostic = AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.MissingScene);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void Resolve_FileScopedTarget_IsFileScoped()
    {
        var resolution = ResolveOne(Jump("chapter-02.md#meet-bob"), new AnchorTable());

        var fileScoped = Assert.IsType<FileScopedJump>(resolution);
        Assert.Equal("chapter-02.md", fileScoped.File);
        Assert.Equal("meet-bob", fileScoped.Anchor);
    }

    [Fact]
    public void Resolve_FileScopedTarget_WarnsThatItLeadsNowhereYet()
    {
        ResolveOne(Jump("chapter-02.md#meet-bob"), new AnchorTable(), out var diagnostics);

        var diagnostic = AssertReported(
            diagnostics.Diagnostics, DiagnosticCatalog.ExternalJumpNotResolved);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void Resolve_FileScopedWithoutAnchor_HasNoAnchor()
    {
        var fileScoped = Assert.IsType<FileScopedJump>(ResolveOne(Jump("chapter-02.md"), new AnchorTable()));

        Assert.Equal("chapter-02.md", fileScoped.File);
        Assert.Null(fileScoped.Anchor);
    }

    [Fact]
    public void Resolve_UrlTarget_IsFileScoped_WarnedNotErrored()
    {
        var fileScoped = Assert.IsType<FileScopedJump>(
            ResolveOne(Jump("http://example.com"), new AnchorTable(), out var diagnostics));

        Assert.Equal("http://example.com", fileScoped.File);
        Assert.Equal(
            DiagnosticSeverity.Warning,
            AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.ExternalJumpNotResolved)
                .Severity);
    }

    [Fact]
    public void Resolve_EmptyTarget_IsUnresolved() =>
        Assert.IsType<UnresolvedJump>(ResolveOne(Jump(string.Empty), new AnchorTable()));

    [Fact]
    public void Resolve_HashOnlyTarget_IsUnresolved() =>
        Assert.IsType<UnresolvedJump>(ResolveOne(Jump("#"), new AnchorTable()));

    [Fact]
    public void Resolve_ReservedEndAnchor_ResolvesToTerminal() =>
        Assert.IsType<TerminalJump>(ResolveOne(Jump("#END"), new AnchorTable()));

    [Fact]
    public void Resolve_LowercaseEnd_IsAnOrdinaryAnchor_NotTerminal()
    {
        // `#end` is a normal slug (a scene titled "End"), never the reserved terminator.
        var anchors = AnchorsFor(SceneHeading("End", 1));

        Assert.IsType<SceneJump>(ResolveOne(Jump("#end"), anchors));
    }

    [Fact]
    public void Resolve_FileScopedEnd_IsFileScoped_NotTerminal()
    {
        var fileScoped = Assert.IsType<FileScopedJump>(
            ResolveOne(Jump("chapter-02.md#END"), new AnchorTable()));

        Assert.Equal("END", fileScoped.Anchor);
    }

    [Fact]
    public void Resolve_KeysEachResolutionByItsJump()
    {
        var anchors = AnchorsFor(SceneHeading("Play tennis", 1));
        var scene = Jump("#play-tennis");
        var external = Jump("chapter-02.md");

        var resolutions = JumpResolver.Resolve([scene, external], anchors, new DiagnosticBag());

        Assert.IsType<SceneJump>(resolutions.Resolve(scene));
        Assert.IsType<FileScopedJump>(resolutions.Resolve(external));
    }

    private static JumpResolution ResolveOne(Jump jump, AnchorTable anchors) =>
        ResolveOne(jump, anchors, out _);

    private static JumpResolution ResolveOne(Jump jump, AnchorTable anchors, out DiagnosticBag diagnostics)
    {
        diagnostics = new DiagnosticBag();
        return JumpResolver.Resolve([jump], anchors, diagnostics).Resolve(jump);
    }

    private static AnchorTable AnchorsFor(params ScriptBlock[] blocks)
    {
        var (_, anchors) = SceneBuilder.Build(
            new DesugaredScriptDocument(new ScriptDocument(blocks)), new DiagnosticBag());
        return anchors;
    }
}
