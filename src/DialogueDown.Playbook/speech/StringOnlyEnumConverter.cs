using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// The built-in string enum converter, configured to accept a name and nothing else.
/// </summary>
/// <remarks>
/// The default allows an enum to be read from a number, which would let a playbook say
/// <c>"style": 1</c> — a document the schema rejects but a lenient reader would play. A format
/// contract is only worth as much as its strictest reader.
/// </remarks>
/// <typeparam name="TEnum">The enum whose members are written by name.</typeparam>
internal sealed class StringOnlyEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringOnlyEnumConverter{TEnum}"/> class.
    /// </summary>
    public StringOnlyEnumConverter()
        : base(namingPolicy: null, allowIntegerValues: false)
    {
    }
}
