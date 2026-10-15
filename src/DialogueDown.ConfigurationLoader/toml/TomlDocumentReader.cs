using DialogueDown.ConfigurationLoader.Document;
using DialogueDown.ConfigurationLoader.Errors;
using Tomlyn.Parsing;
using Tomlyn.Syntax;
using KeySegment = (string Name, DialogueDown.ConfigurationLoader.ConfigurationSourceLocation Location);

namespace DialogueDown.ConfigurationLoader.Toml;

/// <summary>
/// Reads TOML text into a located <see cref="ConfigurationTable"/>, the one type that uses
/// Tomlyn. A syntax error, a duplicate key, or a table defined twice is a located
/// <see cref="DialogueConfigurationException"/> carrying Tomlyn's message.
/// </summary>
/// <remarks>
/// Every spelling of a table reads the same, so each of these gives the same tree:
/// <code>
/// [markdown.unmodeled]
/// table = "keep"
///
/// markdown.unmodeled.table = "keep"
///
/// markdown = { unmodeled = { table = "keep" } }
/// </code>
/// Tomlyn has already rejected every redefinition, so building the tree takes three rules: a
/// dotted key steps into a table, a header steps into the last table of an array of tables, and a
/// table a key or header steps into is created when it does not exist yet.
/// </remarks>
internal sealed class TomlDocumentReader
{
    private readonly string _sourceName;

    private TomlDocumentReader(string sourceName) => _sourceName = sourceName;

    /// <summary>Reads <paramref name="toml"/>, naming <paramref name="sourceName"/> in every location.</summary>
    public static ConfigurationTable Read(string toml, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(toml);
        ArgumentNullException.ThrowIfNull(sourceName);

        var document = SyntaxParser.Parse(toml, sourceName);
        return new TomlDocumentReader(sourceName).ReadDocument(document);
    }

    /// <summary>Steps into the table one key segment names, creating it when it does not exist yet.</summary>
    private static TableBuilder StepInto(TableBuilder table, KeySegment segment) =>
        table.OpenTable(segment.Name, segment.Location, segment.Location);

    private static object ReadScalar(ValueSyntax value) => value switch
    {
        StringValueSyntax text => text.Value!,
        IntegerValueSyntax integer => integer.Value,
        FloatValueSyntax number => number.Value,
        BooleanValueSyntax boolean => boolean.Value,
        _ => ((DateTimeValueSyntax)value).Value.DateTime,
    };

    private ConfigurationTable ReadDocument(DocumentSyntax document)
    {
        if (document.HasErrors)
        {
            // Tomlyn also reports warnings, so the first diagnostic is not always the error.
            var error = document.Diagnostics.First(
                diagnostic => diagnostic.Kind == DiagnosticMessageKind.Error);
            throw new DialogueConfigurationException(error.Message, LocationOf(error.Span));
        }

        var root = new TableBuilder(new ConfigurationSourceLocation(_sourceName, 1, 1));
        AddKeyValues(root, document.KeyValues);
        foreach (var header in document.Tables)
        {
            AddKeyValues(OpenHeader(root, header), header.Items);
        }

        return root.Build();
    }

    /// <summary>Finds or creates the table a <c>[table]</c> or <c>[[array]]</c> header names.</summary>
    private TableBuilder OpenHeader(TableBuilder root, TableSyntaxBase header)
    {
        var path = KeyPath(header.Name!);
        var parent = path.SkipLast(1).Aggregate(root, StepInto);
        var (name, nameLocation) = path[^1];
        var location = LocationOf(header.Span);
        if (header is TableArraySyntax)
        {
            var table = new TableBuilder(location);
            parent.AppendToTableArray(name, nameLocation, location, table);
            return table;
        }

        return parent.OpenTable(name, nameLocation, location);
    }

    private void AddKeyValues(TableBuilder table, IEnumerable<KeyValueSyntax> keyValues)
    {
        foreach (var keyValue in keyValues)
        {
            var path = KeyPath(keyValue.Key!);
            var parent = path.SkipLast(1).Aggregate(table, StepInto);
            var (name, nameLocation) = path[^1];
            parent.Set(name, nameLocation, ReadValue(keyValue.Value!));
        }
    }

    private ConfigurationValue ReadValue(ValueSyntax value) => value switch
    {
        ArraySyntax array => new ConfigurationArray(
            array.Items.Select(item => ReadValue(item.Value!)).ToList(), LocationOf(array.Span)),
        InlineTableSyntax inline => ReadInlineTable(inline),
        _ => new ConfigurationScalar(ReadScalar(value), LocationOf(value.Span)),
    };

    private ConfigurationTable ReadInlineTable(InlineTableSyntax inline)
    {
        var table = new TableBuilder(LocationOf(inline.Span));
        AddKeyValues(table, inline.Items.Select(item => item.KeyValue!));
        return table.Build();
    }

    /// <summary>
    /// The segments of a key and where each was written. A quoted segment is one name, so
    /// <c>a."b.c"</c> has the two segments <c>a</c> and <c>b.c</c>.
    /// </summary>
    private List<KeySegment> KeyPath(KeySyntax key)
    {
        var segments = new List<KeySegment> { Segment(key.Key!) };
        if (key.DotKeys is { } dotted)
        {
            segments.AddRange(dotted.Select(item => Segment(item.Key!)));
        }

        return segments;
    }

    private KeySegment Segment(SyntaxNode key)
    {
        var name = key is BareKeySyntax bare ? bare.Key!.Text! : ((StringValueSyntax)key).Value!;
        return (name, LocationOf(key.Span));
    }

    private ConfigurationSourceLocation LocationOf(SourceSpan span) =>
        new(_sourceName, span.Start.Line + 1, span.Start.Column + 1);

    /// <summary>A value under construction, frozen into a <see cref="ConfigurationValue"/> once the document is read.</summary>
    private abstract class ValueBuilder
    {
        public abstract ConfigurationValue Build();
    }

    /// <summary>A value no later line can add to: a scalar, an inline array, or an inline table.</summary>
    private sealed class FinishedValue(ConfigurationValue value) : ValueBuilder
    {
        public override ConfigurationValue Build() => value;
    }

    /// <summary>The tables of an array of tables, one per <c>[[array]]</c> header.</summary>
    private sealed class TableArrayBuilder(ConfigurationSourceLocation location) : ValueBuilder
    {
        public List<TableBuilder> Tables { get; } = [];

        public override ConfigurationArray Build() =>
            new(Tables.Select(table => (ConfigurationValue)table.Build()).ToList(), location);
    }

    /// <summary>A table that a later header or dotted key can still add to.</summary>
    private sealed class TableBuilder(ConfigurationSourceLocation location) : ValueBuilder
    {
        private readonly List<Slot> _slots = [];

        /// <summary>The table named <paramref name="name"/>, entering the last table of an array of tables.</summary>
        public TableBuilder OpenTable(
            string name, ConfigurationSourceLocation keyLocation, ConfigurationSourceLocation tableLocation)
        {
            switch (Find(name))
            {
                case TableBuilder table:
                    return table;
                case TableArrayBuilder array:
                    return array.Tables[^1];
                default:
                    var created = new TableBuilder(tableLocation);
                    _slots.Add(new Slot(name, keyLocation, created));
                    return created;
            }
        }

        public void AppendToTableArray(
            string name,
            ConfigurationSourceLocation keyLocation,
            ConfigurationSourceLocation arrayLocation,
            TableBuilder table)
        {
            if (Find(name) is not TableArrayBuilder array)
            {
                array = new TableArrayBuilder(arrayLocation);
                _slots.Add(new Slot(name, keyLocation, array));
            }

            array.Tables.Add(table);
        }

        public void Set(string name, ConfigurationSourceLocation keyLocation, ConfigurationValue value) =>
            _slots.Add(new Slot(name, keyLocation, new FinishedValue(value)));

        public override ConfigurationTable Build() =>
            new(_slots.Select(slot => new ConfigurationEntry(slot.Key, slot.KeyLocation, slot.Value.Build())).ToList(), location);

        private ValueBuilder? Find(string name) => _slots.Find(slot => slot.Key == name)?.Value;

        private sealed record Slot(string Key, ConfigurationSourceLocation KeyLocation, ValueBuilder Value);
    }
}
