using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// A build-once index of every node in a desugared tree, grouped by type and connected to its
/// parent, so a compiler sub-pass can query type and ancestry without walking the tree itself.
/// The tree is traversed once; each node is grouped under every type in its inheritance
/// chain (its concrete type up to <see cref="ScriptNode"/>) in document order, so a base-type
/// query such as <c>OfType&lt;Speaker&gt;()</c> works and results read top-to-bottom.
/// </summary>
internal sealed class DialogueTreeIndex
{
    private readonly ILookup<Type, ScriptNode> _byType;
    private readonly IReadOnlyDictionary<ScriptNode, ScriptNode?> _parents;

    private DialogueTreeIndex(
        ILookup<Type, ScriptNode> byType,
        IReadOnlyDictionary<ScriptNode, ScriptNode?> parents)
    {
        _byType = byType;
        _parents = parents;
    }

    /// <summary>
    /// Walks <paramref name="document"/> once and indexes every node by type and parent.
    /// </summary>
    public static DialogueTreeIndex Build(DesugaredScriptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new Builder().CreateIndex(document);
    }

    /// <summary>Every indexed node of type <typeparamref name="T"/>, in document order.</summary>
    public IEnumerable<T> OfType<T>()
        where T : ScriptNode =>
        _byType[typeof(T)].Cast<T>();

    /// <summary>
    /// The indexed node's ancestors, nearest parent first. Parent relationships use reference
    /// identity because separate AST records may carry equal values but sit in different
    /// places in the tree.
    /// </summary>
    public IEnumerable<ScriptNode> AncestorsOf(ScriptNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_parents.TryGetValue(node, out var parent))
        {
            throw new ArgumentException("The node is not part of this index.", nameof(node));
        }

        return EnumerateAncestors(parent);
    }

    private IEnumerable<ScriptNode> EnumerateAncestors(ScriptNode? parent)
    {
        while (parent is not null)
        {
            yield return parent;
            parent = _parents[parent];
        }
    }

    private sealed class Builder
    {
        private readonly List<ScriptNode> _nodes = [];
        private readonly Dictionary<ScriptNode, ScriptNode?> _parents =
            new(ReferenceEqualityComparer.Instance);

        public DialogueTreeIndex CreateIndex(DesugaredScriptDocument document)
        {
            foreach (var block in document.Body)
            {
                Add(block, parent: null);
            }

            // A lookup is a read-only multimap: it keeps document order within each type and
            // returns an empty sequence for a type that was never seen, so OfType needs no
            // missing-key handling.
            var byType = _nodes
                .SelectMany(
                    node => node.TypeChainToScriptNode(),
                    (node, type) => (Type: type, Node: node))
                .ToLookup(entry => entry.Type, entry => entry.Node);

            return new DialogueTreeIndex(byType, _parents);
        }

        private void Add(ScriptNode node, ScriptNode? parent)
        {
            _nodes.Add(node);
            _parents.Add(node, parent);
            foreach (var child in node.Children())
            {
                Add(child, node);
            }
        }
    }
}
