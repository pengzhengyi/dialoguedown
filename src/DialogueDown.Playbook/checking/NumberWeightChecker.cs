using System.Globalization;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Weights;
using static DialogueDown.Playbook.Checking.PlaybookRefusal;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a random choice whose number weights no draw can be made against: a number that is
/// negative or not finite, or numbers that add up to more than a double holds.
/// </summary>
/// <remarks>
/// The compiler never writes such a weight, but a hand-built playbook can: <c>-10</c>, or
/// <c>1e400</c>, which reads as infinite. A draw is made against the total of the shares, so
/// either would leave the draw without meaning. Checking here lets a runtime trust every number
/// a playbook carries; only the numbers the world supplies for key weights are left to check at
/// play time. A guarded random option's number is checked like any other, because a guard decides
/// whether a random option is drawn, not what its weight means.
/// </remarks>
public sealed class NumberWeightChecker : IPlaybookChecker
{
    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        foreach (var node in playbook.Nodes)
        {
            if (node is RandomChoiceNode random)
            {
                CheckNumbers(random);
            }
        }
    }

    private static void CheckNumbers(RandomChoiceNode random)
    {
        var numbers = random.Out
            .OfType<RandomOptionEdge>()
            .Select(option => option.Weight)
            .OfType<NumberWeight>()
            .Select(number => number.Percentage)
            .ToList();

        foreach (var percentage in numbers)
        {
            if (!DrawingWeight.IsNonNegativeAndFinite(percentage))
            {
                Refuse(
                    $"Node {random.Id}, a random choice, has a random option weighted "
                        + $"{percentage.ToString(CultureInfo.InvariantCulture)}%; a number weight must be "
                        + "finite and not negative.");
            }
        }

        if (!double.IsFinite(DrawingWeight.Total(numbers)))
        {
            Refuse(
                $"Node {random.Id}, a random choice, has number weights that add up to more than a "
                    + "double holds; their total must be finite.");
        }
    }
}
