using System.Collections.Immutable;
using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests;

public sealed class StyledTextFragmentTests
{
    [Fact]
    public void RoundTrip_NestedStyling_PreservesTheTree()
    {
        // Styling nests, so the encoding is recursive: bold wrapping italic wrapping text.
        const string Json = """
            {
              "kind": "styled",
              "style": "bold",
              "children": [
                {
                  "kind": "styled",
                  "style": "italic",
                  "children": [
                    {
                      "kind": "text",
                      "text": "very"
                    }
                  ]
                }
              ]
            }
            """;

        PlaybookJsonAssert.AssertRoundTrip<SpeechFragment, StyledTextFragment>(Json);
    }

    [Fact]
    public void Construct_WithoutChildren_IsRejected()
    {
        // Styling that wraps nothing is never produced; accepting it would let a reader
        // render an empty emphasis.
        void EmptyStyling() =>
            _ = new StyledTextFragment(SpeechStyle.Bold, ImmutableArray<SpeechFragment>.Empty);

        Assert.Throws<ArgumentException>(EmptyStyling);
    }

    [Fact]
    public void Construct_WithoutAnInitializedChildArray_IsRejected()
    {
        void Uninitialized() =>
            _ = new StyledTextFragment(SpeechStyle.Bold, default);

        Assert.Throws<ArgumentException>(Uninitialized);
    }
}
