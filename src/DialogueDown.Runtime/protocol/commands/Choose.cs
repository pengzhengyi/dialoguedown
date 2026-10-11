namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The player takes this option.
/// </summary>
/// <remarks>
/// The answer to an <see cref="Offer"/>. The index counts the options in the order the offer listed
/// them, from zero, whatever order the host showed them in. An index the menu did not offer is
/// refused when the run takes it rather than when the command is made, because an index read from
/// outside can be any number.
/// </remarks>
/// <param name="Index">The option's position in the offer, counting from zero.</param>
public sealed record Choose(int Index) : Command;
