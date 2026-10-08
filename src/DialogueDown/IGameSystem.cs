namespace DialogueDown;

/// <summary>
/// An adapter that lets dialogue query game state and execute game commands.
/// </summary>
public interface IGameSystem
{
    public string Query(string query);

    public void Execute(string command);
}
