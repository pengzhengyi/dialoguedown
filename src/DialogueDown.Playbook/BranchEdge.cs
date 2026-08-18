using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// One arm of a block condition, carrying its place in the order the arms are tried.
/// </summary>
/// <param name="Target">The node this arm leads to.</param>
/// <param name="Order">Where this arm sits in the order tried, counting from zero.</param>
/// <param name="Condition">What must hold for the arm to be taken, or <c>null</c> for an else.</param>
public sealed record BranchEdge(int Target, int Order, Condition? Condition) : Edge(Target)
{
    /// <summary>
    /// Gets where this arm sits in the order the arms are tried.
    /// </summary>
    /// <remarks>
    /// Carried explicitly because it is what makes an if/elseif/else chain mean what it says,
    /// and a JSON array does not oblige a reader to preserve order.
    /// </remarks>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("order")]
    public int Order { get; } = Order.AssertNotNegative(nameof(Order));

    /// <summary>
    /// Gets what must hold for the arm to be taken, or <c>null</c> for a final else.
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}
