using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.SpeakerSymbolAssert;

namespace DialogueDown.Tests.Script.Semantics;

public sealed class SpeakerBinderTests
{
    [Fact]
    public void Bind_NameReference_ResolvesByName()
    {
        var table = Bind(SpeakerNameReference("Alice"));

        Assert.Equal("Alice", table.Resolve(SpeakerNameReference("Alice")).Name);
    }

    [Fact]
    public void Bind_IdReference_ResolvesById()
    {
        var table = Bind(SpeakerDeclaration("Alice", "A"), SpeakerIdReference("A"));

        Assert.Equal("A", table.Resolve(SpeakerIdReference("A")).Id);
    }

    [Fact]
    public void Bind_RepeatedNameReference_ConvergesOnOneSymbol()
    {
        var table = Bind(SpeakerNameReference("Alice"), SpeakerNameReference("Alice"));

        Assert.Same(
            table.Resolve(SpeakerNameReference("Alice")),
            table.Resolve(SpeakerNameReference("Alice")));
    }

    [Fact]
    public void Bind_Declaration_BindsNameAndIdToOneSymbol()
    {
        var table = Bind(SpeakerDeclaration("Alice", "A"));

        Assert.Same(
            table.Resolve(SpeakerNameReference("Alice")),
            table.Resolve(SpeakerIdReference("A")));
    }

    [Fact]
    public void Bind_Declaration_MergesCustomTags()
    {
        var table = Bind(SpeakerDeclaration("Alice", tags: CustomTag("happy")));

        AssertTags(table.Resolve(SpeakerNameReference("Alice")), ("happy", null));
    }

    [Fact]
    public void Bind_DefaultTag_MarksTheSpeakerDefault()
    {
        var table = Bind(DefaultSpeakerDeclaration("Narrator"));

        var narrator = table.Resolve(SpeakerNameReference("Narrator"));
        Assert.True(narrator.IsDefault);
        Assert.Same(narrator, table.Resolve(DefaultSpeaker()));
    }

    [Fact]
    public void Bind_OtherReservedTag_IsLeftForTheTagValidator()
    {
        var table = Bind(SpeakerDeclaration("Alice", tags: ReservedTag("vip")));

        var alice = table.Resolve(SpeakerNameReference("Alice"));
        Assert.False(alice.IsDefault);
        Assert.Empty(alice.Tags); // a reserved tag is not a custom tag; the binder ignores it
    }

    [Fact]
    public void Bind_IdReferencedBeforeItsDeclaration_GetsNamed()
    {
        var table = Bind(SpeakerIdReference("A"), SpeakerDeclaration("Alice", "A"));

        Assert.Equal("Alice", table.Resolve(SpeakerIdReference("A")).Name);
    }

    [Fact]
    public void Bind_NameReferencedBeforeItsDeclaration_GetsItsId()
    {
        var table = Bind(SpeakerNameReference("Alice"), SpeakerDeclaration("Alice", "A"));

        Assert.Equal("A", table.Resolve(SpeakerNameReference("Alice")).Id);
        Assert.Same(
            table.Resolve(SpeakerNameReference("Alice")),
            table.Resolve(SpeakerIdReference("A")));
    }

    [Fact]
    public void Bind_PartialDeclaration_MergesTagsIntoTheIdSymbol()
    {
        var table = Bind(
            SpeakerDeclaration("Alice", "A", CustomTag("calm")),
            PartialSpeakerDeclaration("A", CustomTag("excited")));

        var names = table.Resolve(SpeakerIdReference("A")).Tags.Select(tag => tag.Name);
        Assert.Equal(["calm", "excited"], names.OrderBy(name => name));
    }

    [Fact]
    public void Bind_NoDeclaredDefault_FallsBackToAnAnonymousDefault()
    {
        var table = Bind(SpeakerNameReference("Alice"));

        var fallback = table.Resolve(DefaultSpeaker());
        Assert.Null(fallback.Name);
        Assert.True(fallback.IsDefault);
    }

    [Fact]
    public void Bind_DefaultSpeakerNode_IsIgnored()
    {
        var table = Bind(DefaultSpeaker());

        Assert.Null(table.Resolve(DefaultSpeaker()).Name);
    }

    [Fact]
    public void Bind_ConfigLayerDefault_NoScriptDefault_UsesTheConfigDefault()
    {
        var table = BindLayers(
            configured: [DefaultSpeakerDeclaration("Narrator")],
            script: [SpeakerNameReference("Alice")]);

        var resolved = table.Resolve(DefaultSpeaker());
        Assert.Equal("Narrator", resolved.Name);
        Assert.True(resolved.IsDefault);
    }

    [Fact]
    public void Bind_ScriptDefault_OverridesTheConfigDefault()
    {
        var table = BindLayers(
            configured: [DefaultSpeakerDeclaration("Narrator")],
            script: [DefaultSpeakerDeclaration("Bob")]);

        Assert.Equal("Bob", table.Resolve(DefaultSpeaker()).Name);
        Assert.False(table.Resolve(SpeakerNameReference("Narrator")).IsDefault);
    }

    [Fact]
    public void Bind_ConfigAndScript_SameName_ConvergeOnOneDefault()
    {
        var table = BindLayers(
            configured: [DefaultSpeakerDeclaration("Narrator")],
            script: [SpeakerNameReference("Narrator")]);

        var narrator = table.Resolve(SpeakerNameReference("Narrator"));
        Assert.True(narrator.IsDefault);
        Assert.Same(narrator, table.Resolve(DefaultSpeaker()));
    }

    [Fact]
    public void Bind_TwoDefaultsWithinTheConfigLayer_Reports()
    {
        BindLayers(
            out var diagnostics,
            configured: [DefaultSpeakerDeclaration("Narrator"), DefaultSpeakerDeclaration("Alice")],
            script: []);

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.MultipleDefaultSpeakers);
    }

    [Fact]
    public void Add_UnknownSpeakerKind_Throws()
    {
        var binder = new SpeakerBinder(new DiagnosticBag());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => binder.Add(new UnknownSpeaker(SourceSpanFactory.Span())));
    }

    [Fact]
    public void Bind_NameBoundToTwoIds_Reports()
    {
        Bind(out var diagnostics, SpeakerDeclaration("Alice", "A"), SpeakerDeclaration("Alice", "B"));

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.NameBoundToAnotherId);
    }

    [Fact]
    public void Bind_IdBoundToTwoNames_Reports()
    {
        Bind(out var diagnostics, SpeakerDeclaration("Alice", "A"), SpeakerDeclaration("Bob", "A"));

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.IdBoundToAnotherName);
    }

    [Fact]
    public void Bind_FusingTwoSeparatelyUsedSpeakers_Reports()
    {
        Bind(
            out var diagnostics,
            SpeakerNameReference("Alice"),
            SpeakerIdReference("A"),
            SpeakerDeclaration("Alice", "A"));

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.SpeakerNameIdConflict);
    }

    [Fact]
    public void Bind_TwoDefaultSpeakers_Reports()
    {
        Bind(out var diagnostics, DefaultSpeakerDeclaration("Alice"), DefaultSpeakerDeclaration("Bob"));

        AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.MultipleDefaultSpeakers);
    }

    [Fact]
    public void Bind_SameSpeakerMarkedDefaultTwice_IsAllowed()
    {
        var table = Bind(
            SpeakerDeclaration("Alice", "A", ReservedTag("default")),
            PartialSpeakerDeclaration("A", ReservedTag("default")));

        Assert.True(table.Resolve(SpeakerNameReference("Alice")).IsDefault);
    }

    [Fact]
    public void Bind_TwoDefaults_LabelsAnIdOnlyDefaultByItsId()
    {
        // The first default is an @id partial (no name yet); the diagnostic names both the
        // established default (@A) and the offending one (Bob), labelling the id-only one by its id.
        Bind(
            out var diagnostics,
            PartialSpeakerDeclaration("A", ReservedTag("default")),
            DefaultSpeakerDeclaration("Bob"));

        var diagnostic = AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.MultipleDefaultSpeakers);
        Assert.Equal("@A", diagnostic.MessageArguments[0]);
        Assert.Equal("Bob", diagnostic.MessageArguments[1]);
    }

    [Fact]
    public void Bind_IdNeverGivenAName_Reports()
    {
        var reference = SpeakerIdReference("A") with { Span = new SourceSpan(7, 2) };

        Bind(out var diagnostics, reference);

        // points at where @A was first used
        Assert.Equal(reference.Span, AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.UnnamedSpeakerId).Span);
    }

    private static SpeakerTable Bind(params Speaker[] speakers) => Bind(out _, speakers);

    private static SpeakerTable Bind(out DiagnosticBag diagnostics, params Speaker[] speakers)
    {
        diagnostics = new DiagnosticBag();
        return SpeakerBinder.Bind(speakers, diagnostics);
    }

    private static SpeakerTable BindLayers(Speaker[] configured, Speaker[] script) =>
        BindLayers(out _, configured, script);

    private static SpeakerTable BindLayers(
        out DiagnosticBag diagnostics, Speaker[] configured, Speaker[] script)
    {
        diagnostics = new DiagnosticBag();
        return SpeakerBinder.Bind(configured, script, diagnostics);
    }

    private sealed record UnknownSpeaker(SourceSpan Span) : Speaker(Span);
}
