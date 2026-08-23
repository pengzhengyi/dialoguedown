using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class PlaybookDocumentTests
{
    [Fact]
    public void Anchors_HoweverTheyWereSorted_ReadInOrdinalOrder()
    {
        // The default comparer follows the current culture, which puts "a" before "B" in one
        // place and after it in another. A playbook's order must not depend on the machine.
        var byCulture = new[] { "B", "a" }.ToImmutableSortedDictionary(slug => slug, _ => 0);

        var playbook = new PlaybookDocument(
            PlaybookFactory.Format(), "chapter-01.dialogue.md", 0, byCulture, [], [new EndNode(0)]);

        Assert.Equal(["B", "a"], playbook.Anchors.Keys);
    }


    [Fact]
    public void RoundTrip_AWholePlaybook_PreservesEveryTable()
    {
        const string Json = """
            {
              "$schema": "https://pengzhengyi.github.io/dialoguedown/schema/playbook-0.schema.json",
              "format": {
                "version": 0,
                "requires": [
                  "core"
                ],
                "uses": []
              },
              "script": "chapter-01.dialogue.md",
              "entry": 0,
              "anchors": {
                "the-inn": 1
              },
              "speakers": [
                {
                  "id": "alice",
                  "name": "Alice",
                  "tags": []
                }
              ],
              "nodes": [
                {
                  "kind": "line",
                  "id": 0,
                  "speaker": 0,
                  "speech": [
                    {
                      "kind": "text",
                      "text": "Welcome."
                    }
                  ],
                  "out": [
                    {
                      "kind": "succession",
                      "target": 1
                    }
                  ]
                },
                {
                  "kind": "end",
                  "id": 1,
                  "out": []
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<PlaybookDocument>(Json);
    }

    [Fact]
    public void Write_TablesInAnyOrder_AlwaysWritesThemSorted()
    {
        // Entries and anchors are lookup tables, so their order carries no meaning — but a golden
        // file needs one. Sorting makes a playbook byte-identical however the writer built it.
        var document = PlaybookFactory.Document(anchors: [("the-inn", 1), ("a-market", 2)]);

        var json = PlaybookJsonAssert.Serialize(document);

        Assert.True(
            json.IndexOf("a-market", StringComparison.Ordinal)
                < json.IndexOf("the-inn", StringComparison.Ordinal),
            "anchors should be written in sorted order");
    }

    [Fact]
    public void Construct_WithoutAFormat_IsRejected()
    {
        // Without a header nothing can decide whether the document is playable at all.
        Assert.Throws<ArgumentNullException>(
            () => new PlaybookDocument(null!, "chapter-01.dialogue.md", 0, null!, [], []));
    }
}
