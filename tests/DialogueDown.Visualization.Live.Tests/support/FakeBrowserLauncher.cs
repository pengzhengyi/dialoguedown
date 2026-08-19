namespace DialogueDown.Visualization.Live.Tests.Support;

/// <summary>Records the targets it is asked to open, without launching anything.</summary>
internal sealed class FakeBrowserLauncher : IBrowserLauncher
{
    /// <summary>Targets passed to <see cref="Open"/>, in order.</summary>
    public List<string> Opened { get; } = [];

    public void Open(string target) => Opened.Add(target);
}
