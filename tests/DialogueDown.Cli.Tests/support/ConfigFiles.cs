namespace DialogueDown.Cli.Tests.Support;

/// <summary>The text of <c>dialogue.toml</c> files the command tests write into a project.</summary>
internal static class ConfigFiles
{
    /// <summary>A project whose lines without a speaker are said by the Narrator.</summary>
    public const string NarratorByDefault = """
        [[speakers]]
        name = "Narrator"
        default = true
        """;
}
