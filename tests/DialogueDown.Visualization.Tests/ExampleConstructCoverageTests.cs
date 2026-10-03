using System.Reflection;
using DialogueDown.Compilation;
using DialogueDown.Script.Ast;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests;

/// <summary>
/// Every construct the language models is demonstrated by a shipped example.
/// </summary>
/// <remarks>
/// A construct no example uses has nothing showing a reader how to write it, and no golden file
/// recording how it compiles. The check reads the AST the compiler produced, so adding a construct
/// fails this test until an example uses it.
/// </remarks>
public sealed class ExampleConstructCoverageTests
{
    // Constructs no example can demonstrate: the document root that wraps every parse.
    private static readonly HashSet<string> _notAuthored = new(StringComparer.Ordinal)
    {
        nameof(ScriptDocument),
    };

    [Fact]
    public void EveryConstruct_AppearsInAnExample()
    {
        var demonstrated = DemonstratedConstructs();
        var undemonstrated = ModeledConstructs()
            .Where(name => !demonstrated.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            undemonstrated.Count == 0,
            $"{undemonstrated.Count} construct(s) appear in no example: "
                + string.Join(", ", undemonstrated)
                + ". Add a natural use to an example, so readers can see the construct written down "
                + "and its playbook golden pins how it compiles.");
    }

    [Fact]
    public void TheExamples_ExerciseMoreThanAHandfulOfConstructs()
    {
        // A walk that stopped descending would leave the check above passing against almost
        // nothing.
        Assert.True(DemonstratedConstructs().Count > 20);
    }

    /// <summary>Every construct the compiler can put in an AST, by type name.</summary>
    private static IEnumerable<string> ModeledConstructs() =>
        UnionMembers.NamesOf<ScriptNode>()
            .Where(name => !_notAuthored.Contains(name))
            .Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Every construct the shipped examples reach, across both trees the report shows: what the
    /// author wrote (the Dialogue AST) and what desugaring made of it. Some constructs — an
    /// assembled jump, a filled-in default speaker — exist only after desugaring, so reading the
    /// first tree alone would report them as undemonstrated.
    /// </summary>
    private static HashSet<string> DemonstratedConstructs()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var example in ExampleScripts.Names())
        {
            var result = Pipeline.Compilation(ExampleScripts.Read(example));
            if (result.ReachedDesugared() is { } desugared)
            {
                Collect(desugared, seen);
            }

            Collect(Pipeline.Document(ExampleScripts.Read(example)), seen);
        }

        return seen;
    }

    // Walks a node's public properties for nested nodes, so a new construct is found without a
    // hand-written list.
    private static void Collect(object node, HashSet<string> seen)
    {
        if (node is ScriptNode)
        {
            seen.Add(node.GetType().Name);
        }

        foreach (var property in node.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            switch (property.GetValue(node))
            {
                case ScriptNode child:
                    Collect(child, seen);
                    break;
                case System.Collections.IEnumerable items and not string:
                    foreach (var item in items)
                    {
                        if (item is ScriptNode child)
                        {
                            Collect(child, seen);
                        }
                    }

                    break;
            }
        }
    }
}
