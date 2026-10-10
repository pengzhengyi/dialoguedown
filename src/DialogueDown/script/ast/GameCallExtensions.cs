namespace DialogueDown.Script.Ast;

/// <summary>
/// Reads a <see cref="GameCall"/> back as text, for messages that show a writer the call they
/// wrote.
/// </summary>
internal static class GameCallExtensions
{
    /// <summary>
    /// The call in its standard written form, the one a script uses: <c>"PlaceName"</c> for a
    /// query, <c>("wave")</c> for a default command, and <c>GiveQuest("EmberCrown", "3")</c> for
    /// a named command.
    /// </summary>
    /// <remarks>
    /// The form is rebuilt from the call's parts, so spacing the writer added inside the backticks
    /// is not kept.
    /// </remarks>
    public static string Canonical(this GameCall call) => call switch
    {
        Query query => Quoted(query.Key),
        DefaultCommand command => $"({Quoted(command.Action)})",
        CustomCommand command => $"{command.Name}({string.Join(", ", command.Args.Select(Quoted))})",
        _ => throw new NotSupportedException($"No written form is defined for {call.GetType().Name}."),
    };

    // A quoted string cannot contain a quote, so wrapping one needs no escaping.
    private static string Quoted(string text) => $"\"{text}\"";
}
