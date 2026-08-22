using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using Ast = DialogueDown.Script.Ast;

namespace DialogueDown.Emission;

/// <summary>
/// Writes the game calls a line or a control block runs — its effects.
/// </summary>
/// <remarks>
/// Separate from the rest of speech because an effect is a different thing to a host: text is
/// shown, an effect is performed. A line's effects stay in its speech, in position, so a runtime
/// knows where in the line each one fires.
/// </remarks>
internal static class EffectMapping
{
    /// <summary>Writes a list of effects, in the order they run.</summary>
    /// <param name="calls">The calls to write.</param>
    /// <returns>The same effects as a playbook carries them.</returns>
    public static ImmutableArray<SpeechFragment> Write(IReadOnlyList<Ast.GameCall> calls)
    {
        ArgumentNullException.ThrowIfNull(calls);

        return [.. calls.Select(Write)];
    }

    /// <summary>Writes one effect.</summary>
    /// <param name="call">The call to write.</param>
    /// <returns>The same effect as a playbook carries it.</returns>
    public static SpeechFragment Write(Ast.GameCall call)
    {
        ArgumentNullException.ThrowIfNull(call);

        return call switch
        {
            Ast.Query query => new QueryFragment(query.Key),
            Ast.DefaultCommand command => new DefaultCommandFragment(command.Action),
            Ast.CustomCommand custom => new CustomCommandFragment(custom.Name, [.. custom.Args]),
            _ => throw new NotSupportedException(
                $"No playbook fragment is defined for {call.GetType().Name}."),
        };
    }
}
