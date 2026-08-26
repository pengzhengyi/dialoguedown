using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Configuration;

/// <summary>
/// A configured speaker as shown in the Config tab: its <see cref="Name"/>, optional
/// <see cref="Id"/>, and its <see cref="Tags"/> (custom first, then reserved) each carrying a
/// reserved/custom flag for coloring.
/// </summary>
internal sealed record ConfiguredSpeakerView(
    string Name, string? Id, IReadOnlyList<TagView> Tags);
