using System.Collections.Immutable;
using DialogueDown.Compilation;
using DialogueDown.Configuration;
using DialogueDown.Markdown;
using Microsoft.Extensions.DependencyInjection;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// A project's configured unmodeled-Markdown handling reaches a real compile: a table is ignored
/// by default, and a project that configures it to be kept finds its text in the document. Guards
/// the wiring from <see cref="CompilerOptions"/> through each composition root into the front end,
/// which a policy-only test cannot see.
/// </summary>
public sealed class ConfiguredUnmodeledMarkdownCompilationTests
{
    private const string ScriptWithTable = """
        # The Tavern

        | Speaker | Mood |
        | --- | --- |
        | Keeper | wary |

        Keeper: Welcome.
        """;

    // The text fragments the front end keeps from the script, joined by newlines, with and
    // without the table.
    private const string WithoutTheTable = """
        The Tavern
        Keeper: Welcome.
        """;

    private const string WithTheTable = """
        The Tavern
        | Speaker | Mood |
        | --- | --- |
        | Keeper | wary |
        Keeper: Welcome.
        """;

    private static readonly CompilerOptions _keepsTables = CompilerOptions.Default with
    {
        UnmodeledMarkdown = ImmutableDictionary<UnmodeledNodeKind, UnmodeledNodeHandling>.Empty
            .Add(UnmodeledNodeKind.Table, UnmodeledNodeHandling.Keep),
    };

    [Fact]
    public void CreateDefault_Unconfigured_IgnoresTheTable() =>
        Assert.Equal(WithoutTheTable, TextOf(Compile(ScriptCompilerFactory.CreateDefault())));

    [Fact]
    public void CreateDefault_ConfiguredToKeepTables_KeepsTheTableText() =>
        Assert.Equal(WithTheTable, TextOf(Compile(ScriptCompilerFactory.CreateDefault(_keepsTables))));

    [Fact]
    public void AddDialogueDown_ConfiguredToKeepTables_KeepsTheTableText()
    {
        // The container root must honor the setting too, or the two roots would disagree.
        using var provider = new ServiceCollection()
            .AddDialogueDown(_keepsTables)
            .BuildServiceProvider();

        var result = Compile(provider.GetRequiredService<IScriptCompiler>());

        Assert.Equal(WithTheTable, TextOf(result));
    }

    [Fact]
    public void AddDialogueDown_Unconfigured_IgnoresTheTable()
    {
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        var result = Compile(provider.GetRequiredService<IScriptCompiler>());

        Assert.Equal(WithoutTheTable, TextOf(result));
    }

    [Fact]
    public void CreateDefault_ConfiguredToKeepOneKind_LeavesTheOtherDefaultsAlone()
    {
        // Keeping tables must not also start keeping dividers, so the `***` leaves no text behind.
        const string Script = """
            # The Tavern

            ***

            | Speaker | Mood |
            | --- | --- |
            | Keeper | wary |
            """;

        var text = TextOf(ScriptCompilerFactory.CreateDefault(_keepsTables).Compile(Script));

        Assert.Equal(
            """
            The Tavern
            | Speaker | Mood |
            | --- | --- |
            | Keeper | wary |
            """,
            text);
    }

    [Fact]
    public void CreateDefault_Unconfigured_IgnoresALinkReferenceDefinition()
    {
        // A link reference definition is CommonMark plumbing, not a spoken line, so it leaves no
        // text behind.
        const string Script = """
            # A

            Guide: Hi.

            [the market]: #b

            # B

            Guide: There.
            """;

        var text = TextOf(ScriptCompilerFactory.CreateDefault().Compile(Script));

        Assert.Equal(
            """
            A
            Guide: Hi.
            B
            Guide: There.
            """,
            text);
    }

    private static CompilationResult Compile(IScriptCompiler compiler) =>
        compiler.Compile(ScriptWithTable);

    // Every text fragment the front end kept, which is where the handling policy decides.
    private static string TextOf(CompilationResult result) =>
        string.Join("\n", result.Markdown.Blocks.SelectMany(Texts));

    private static IEnumerable<string> Texts(MarkdownBlock block) => block switch
    {
        Paragraph paragraph => paragraph.Inlines.OfType<TextInline>().Select(text => text.Text),
        Heading heading => heading.Inlines.OfType<TextInline>().Select(text => text.Text),
        _ => [],
    };
}
