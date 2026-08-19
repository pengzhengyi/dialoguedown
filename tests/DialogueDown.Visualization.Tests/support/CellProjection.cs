using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// A configurable projection over <see cref="Cell"/>. Edges and attributes live
/// in side tables keyed by reference identity, so a test can wire arbitrary
/// trees, shared nodes, and cycles — including value-equal siblings — without the
/// node type carrying its own structure.
/// </summary>
internal sealed class CellProjection : INodeProjection<Cell>
{
    private readonly Dictionary<Cell, IReadOnlyList<Cell>> _neighbors =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Cell, IReadOnlyList<DisplayAttribute>> _attributes =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Cell, string> _sources =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Cell, string> _categories =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Cell, string> _typeNames =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Cell, string> _refKeys =
        new(ReferenceEqualityComparer.Instance);

    public string Title { get; init; } = "Cells";

    public string Description { get; init; } = "A cell graph.";

    public CellProjection Link(Cell from, params Cell[] to)
    {
        _neighbors[from] = to;
        return this;
    }

    public CellProjection WithAttributes(Cell node, params DisplayAttribute[] attributes)
    {
        _attributes[node] = attributes;
        return this;
    }

    public CellProjection WithSource(Cell node, string source)
    {
        _sources[node] = source;
        return this;
    }

    public CellProjection WithCategory(Cell node, string category)
    {
        _categories[node] = category;
        return this;
    }

    public CellProjection WithTypeName(Cell node, string typeName)
    {
        _typeNames[node] = typeName;
        return this;
    }

    public CellProjection WithRefKey(Cell node, string refKey)
    {
        _refKeys[node] = refKey;
        return this;
    }

    public NodeDescription Describe(Cell node) =>
        new(
            node.Name,
            _attributes.TryGetValue(node, out var attributes) ? attributes : null,
            _sources.TryGetValue(node, out var source) ? source : null,
            _categories.TryGetValue(node, out var category) ? category : null,
            typeName: _typeNames.TryGetValue(node, out var typeName) ? typeName : null,
            refKey: _refKeys.TryGetValue(node, out var refKey) ? refKey : null);

    public IEnumerable<Cell> Neighbors(Cell node) =>
        _neighbors.TryGetValue(node, out var neighbors) ? neighbors : [];
}
