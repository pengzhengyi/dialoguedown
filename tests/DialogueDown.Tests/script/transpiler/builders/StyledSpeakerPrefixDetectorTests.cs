using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class StyledSpeakerPrefixDetectorTests
{
    [Fact]
    public void Report_AnItalicName_ReportsAStyledSpeakerPrefixWarning()
    {
        var diagnostic = Assert.Single(Check(Italic(Text("Alice")), Text(": Hello there.")));

        Assert.Equal(DiagnosticCatalog.StyledSpeakerPrefix.Code, diagnostic.Descriptor.Code);
        Assert.Equal(DiagnosticCategory.Syntax, diagnostic.Descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Alice:", Assert.Single(diagnostic.MessageArguments));
    }

    [Fact]
    public void Report_ABoldName_Reports() =>
        Assert.Single(Check(Emphasis(EmphasisKind.Bold, Text("Bob")), Text(": Hi")));

    [Fact]
    public void Report_AStrikethroughName_Reports() =>
        Assert.Single(Check(Emphasis(EmphasisKind.Strikethrough, Text("Ghost")), Text(": ...")));

    [Fact]
    public void Report_AStyledNameWithATag_Reports()
    {
        var diagnostic = Assert.Single(Check(Italic(Text("Alice")), Text(" #excited: hi")));

        Assert.Equal("Alice #excited:", Assert.Single(diagnostic.MessageArguments));
    }

    [Fact]
    public void Report_StylingWithinTheName_Reports()
    {
        // Only part of the name is styled, but the flattened run is still a prefix.
        var diagnostic = Assert.Single(Check(Text("A"), Italic(Text("l")), Text("ice: hi")));

        Assert.Equal("Alice:", Assert.Single(diagnostic.MessageArguments));
    }

    [Fact]
    public void Report_AFullyStyledLine_ReportsNothing() =>
        // The colon is inside the styling, so the styled run does not end before it.
        Assert.Empty(Check(Italic(Text("Alice: hi"))));

    [Fact]
    public void Report_AnUnstyledPrefixShape_ReportsNothing() =>
        // No styling means an ordinary line, not a mis-styled prefix.
        Assert.Empty(Check(Text("hello: world")));

    [Fact]
    public void Report_AStyledNonName_ReportsNothing() =>
        // A multi-word run is not a valid speaker name, so it does not parse as a prefix.
        Assert.Empty(Check(Italic(Text("the great")), Text(": hi")));

    [Fact]
    public void Report_AStyledRunWithoutAColon_ReportsNothing() =>
        Assert.Empty(Check(Italic(Text("Alice")), Text(" waves")));

    [Fact]
    public void Compiling_AStyledSpeakerPrefix_LocatesTheWarningAtTheWouldBePrefix()
    {
        var source = "*Alice*: Hello there.";

        var compiler = ScriptCompilerFactory.CreateDefault(
            CompilerOptions.Default with { Mode = CompilationMode.BestEffort });
        var diagnostic = Assert.Single(
            compiler.Compile(source).LocatedDiagnostics,
            located => located.Code == DiagnosticCatalog.StyledSpeakerPrefix.Code);

        Assert.Equal(
            "*Alice*:",
            source.Substring(diagnostic.StartOffset, diagnostic.EndOffset - diagnostic.StartOffset));
    }

    private static IReadOnlyList<Diagnostic> Check(params MarkdownInline[] leading)
    {
        var bag = new DiagnosticBag();
        StyledSpeakerPrefixDetector.Report(leading, bag);
        return bag.Diagnostics;
    }

    private static TextInline Text(string text) => new(text, SourceSpanFactory.Span());

    private static EmphasisInline Italic(params MarkdownInline[] children) =>
        Emphasis(EmphasisKind.Italic, children);

    private static EmphasisInline Emphasis(EmphasisKind kind, params MarkdownInline[] children) =>
        new(kind, children, SourceSpanFactory.Span());
}
