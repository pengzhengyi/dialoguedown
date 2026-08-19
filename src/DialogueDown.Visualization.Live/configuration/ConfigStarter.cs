namespace DialogueDown.Visualization.Live.Configuration;

/// <summary>
/// The seed text a newly created <c>dialogue.toml</c> is written with. It is a friendly,
/// fully commented scaffold: it teaches the <c>mode</c> setting and the <c>[[speakers]]</c> schema
/// by example, yet the project keeps compiling on the built-in defaults until the reader uncomments
/// it — so creating a config has no effect on the resolved options until they choose to edit.
/// </summary>
internal static class ConfigStarter
{
    /// <summary>The commented starter scaffold: a worked <c>mode</c> and <c>[[speakers]]</c> example.</summary>
    public const string Template =
        """
        # Compilation mode for this project — how far a compile proceeds after an error. It applies
        # to the dialoguedown CLI and to embedded builds:
        #   stage-boundary  - stop at the first stage that reports an error (the default)
        #   best-effort     - recover through every stage and collect every problem
        # The visualization always renders stage-boundary, so every stage it shows is built from
        # reliable input; this setting does not change what the report displays. In Edit,
        # autocompletion suggests the key and its values.

        # mode = "stage-boundary"

        # Speakers this project's scripts can use. Each [[speakers]] block is one speaker:
        #   name     - how the speaker is shown (required)
        #   id       - an optional short id, referenced as @id in a script
        #   tags     - optional custom tags
        #   default  - set true to make this the document's default speaker (the ##default tag)
        #
        # Uncomment the example below, or add your own. In Edit, autocompletion suggests the keys.

        # [[speakers]]
        # name = "Narrator"
        # id = "N"
        # tags = ["calm"]
        # default = true

        """;
}
