using DialogueDown.Playbook;
using DialogueDown.Playbook.Checking;

namespace DialogueDown.TestSupport;

/// <summary>Assertions about a whole playbook.</summary>
public static class PlaybookAssert
{
    /// <summary>Asserts that the default reader accepts a playbook.</summary>
    /// <remarks>A refusal fails the test with the reader's reason.</remarks>
    /// <param name="playbook">The playbook to read.</param>
    public static void AssertValidPlaybook(PlaybookDocument playbook) =>
        PlaybookCheckerFactory.CreateDefault().Check(playbook);
}
