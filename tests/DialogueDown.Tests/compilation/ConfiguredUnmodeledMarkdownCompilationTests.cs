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

    private static readonly CompilerOptions _keepsTables = CompilerOptions.Default with
    {
        UnmodeledMarkdown = ImmutableDictionary<UnmodeledNodeKind, UnmodeledNodeHandling>.Empty
            .Add(UnmodeledNodeKind.Table, UnmodeledNodeHandling.Keep),
    };

    [Fact]
    public void CreateDefault_Unconfigured_IgnoresTheTable() =>
        Assert.DoesNotContain("wary", TextOf(Compile(ScriptCompilerFactory.CreateDefault())));

    [Fact]
    public void CreateDefault_ConfiguredToKeepTables_KeepsTheTableText() =>
        Assert.Contains("wary", TextOf(Compile(ScriptCompilerFactory.CreateDefault(_keepsTables))));

    [Fact]
    public void AddDialogueDown_ConfiguredToKeepTables_KeepsTheTableText()
    {
        // The container root must honor the setting too, or the two roots would disagree.
        using var provider = new ServiceCollection()
            .AddDialogueDown(_keepsTables)
            .BuildServiceProvider();

        var result = Compile(provider.GetRequiredService<IScriptCompiler>());

        Assert.Contains("wary", TextOf(result));
    }

    [Fact]
    public void AddDialogueDown_Unconfigured_IgnoresTheTable()
    {
        using var provider = new ServiceCollection().AddDialogueDown().BuildServiceProvider();

        var result = Compile(provider.GetRequiredService<IScriptCompiler>());

        Assert.DoesNotContain("wary", TextOf(result));
    }

    [Fact]
    public void CreateDefault_ConfiguredToKeepOneKind_LeavesTheOtherDefaultsAlone()
    {
        // Keeping tables must not also start keeping dividers. The divider is written `***` so it
        // cannot be confused with a table's own `| --- |` separator row, which survives inside the
        // kept table text.
        const string Script = """
            # The Tavern

            ***

            | Speaker | Mood |
            | --- | --- |
            | Keeper | wary |
            """;

        var text = TextOf(ScriptCompilerFactory.CreateDefault(_keepsTables).Compile(Script));

        Assert.Contains("wary", text);
        Assert.DoesNotContain("***", text);
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
