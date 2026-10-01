using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Parsed;
using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Builds a <see cref="GameCall"/> from a code span's inner text. The whole text must be one
/// game call: text that is not reports <see cref="DiagnosticCatalog.NotAGameCall"/> and recovers
/// by keeping the code span's text as literal speech. The node carries the code span's source span.
/// </summary>
internal sealed class GameCallBuilder(IParser<GameCallData> parser)
{
    public InlineFragment Build(ParseInput input, SourceSpan span, IDiagnosticSink diagnostics)
    {
        var result = parser.ConsumeAll(input);
        if (result.Success)
        {
            return ToNode(result.MatchedValue, span);
        }

        diagnostics.Report(new Diagnostic(DiagnosticCatalog.NotAGameCall, span, [input.Text]));
        return new Text(input.Text, span);
    }

    private static GameCall ToNode(GameCallData data, SourceSpan span)
    {
        if (data is QueryData query)
        {
            return new Query(query.Key, span);
        }

        if (data is DefaultCommandData defaultCommand)
        {
            return new DefaultCommand(defaultCommand.Action, span);
        }

        var custom = (CustomCommandData)data;
        return new CustomCommand(custom.Name, custom.Args, span);
    }
}
