using System.Runtime.CompilerServices;

namespace DialogueDown.Tests.Support;

/// <summary>
/// The checkout this suite was built from, for a test that compares a committed document with
/// what the code says it should hold.
/// </summary>
internal static class Repository
{
    /// <summary>
    /// The checkout's root directory: the nearest directory above this file's source location
    /// that holds the solution file, so it is found wherever the runner places the compiled
    /// binaries.
    /// </summary>
    public static string Root => FindRoot(ThisFile());

    private static string FindRoot(string sourcePath)
    {
        for (var directory = new FileInfo(sourcePath).Directory; directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DialogueDown.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (DialogueDown.sln).");
    }

    private static string ThisFile([CallerFilePath] string path = "") => path;
}
