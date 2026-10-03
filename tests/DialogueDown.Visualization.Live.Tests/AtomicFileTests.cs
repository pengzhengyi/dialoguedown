using System.Text;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Live.Files;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class AtomicFileTests
{
    [Fact]
    public void Transact_ReadsTheCurrentContentUnderTheLock()
    {
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "on disk");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Equal("on disk", seen);
    }

    [Fact]
    public void Transact_MissingFile_ReportsNullAndLeavesNoFileWhenNotWritten()
    {
        using var tree = new TempTree();
        var path = Path.Combine(tree.Root, "missing.txt");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Null(seen);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Transact_Write_CommitsContentWithoutABom()
    {
        using var tree = new TempTree();
        var path = Path.Combine(tree.Root, "created.txt");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        Assert.False(File.ReadAllBytes(path).Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
    }

    [Fact]
    public void Transact_Write_TruncatesAShorterReplacement()
    {
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "a long original line");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("short");
            return 0;
        });

        Assert.Equal("short", File.ReadAllText(path));
    }

    [Fact]
    public void Transact_MissingDirectory_ReportsNullWithoutWriting()
    {
        using var tree = new TempTree();
        var path = Path.Combine(tree.Root, "nope", "doc.txt");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Null(seen);
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public void Transact_MissingFileCreatedConcurrently_IsReadAndNeverDeleted()
    {
        // The target does not exist when the transaction begins, and an external writer creates
        // it while the body runs.
        using var tree = new TempTree();
        var path = Path.Combine(tree.Root, "raced.txt");

        var seen = AtomicFile.Transact(path, transaction =>
        {
            File.WriteAllText(path, "external content");
            return transaction.Disk;
        });

        Assert.Null(seen); // it did not exist when the snapshot was read
        Assert.True(File.Exists(path));
        Assert.Equal("external content", File.ReadAllText(path));
    }

    [Fact]
    public void Transact_Write_LeavesNoStagingTempBehind()
    {
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(tree.Root));
    }

    [Fact]
    public void Transact_Write_StagesInTheSameDirectory()
    {
        // A temp file beside the target keeps the final move on one volume, where it is an atomic
        // rename.
        using var tree = new TempTree();
        var directory = tree.Dir("nested");
        var path = Path.Combine(directory, "doc.txt");
        File.WriteAllText(path, "original");

        string? stagingDirectory = null;
        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            stagingDirectory = Directory.GetFiles(directory)
                .FirstOrDefault(file => !string.Equals(file, path, StringComparison.Ordinal)) is { } temp
                ? Path.GetDirectoryName(temp)
                : null;
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        if (stagingDirectory is not null)
        {
            Assert.Equal(directory, stagingDirectory);
        }
    }

    [Fact]
    public void Transact_ValidatedWrite_ExternalEditInTheReplaceWindow_ThrowsConflictAndKeepsTheExternalContent()
    {
        // An external editor writes the target after the snapshot is read but before the staged
        // replacement lands. Writing inside the body hits that window, because the commit runs
        // after the body returns.
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        Assert.Throws<AtomicFile.WriteConflictException>(() => AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("my staged replacement");
            File.WriteAllText(path, "external edit");
            return 0;
        }));

        Assert.Equal("external edit", File.ReadAllText(path)); // rolled back to the external content
        Assert.Single(Directory.GetFiles(tree.Root)); // no leftover temp or backup
    }

    [Fact]
    public void Transact_ValidatedCreate_ExternalCreateInTheWindow_ThrowsConflictAndKeepsTheExternalFile()
    {
        // A create (the snapshot reports the file absent) must use a no-overwrite move, so an
        // external process that creates the file first wins the race and is never clobbered.
        using var tree = new TempTree();
        var path = Path.Combine(tree.Root, "new.txt");

        Assert.Throws<AtomicFile.WriteConflictException>(() => AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("my staged create");
            File.WriteAllText(path, "external create"); // appears before the no-overwrite move
            return 0;
        }));

        Assert.Equal("external create", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(tree.Root));
    }

    [Fact]
    public void Transact_ForcedWrite_OverwritesAnExternalEditInTheWindow()
    {
        // A forced write is an overwrite the user confirmed, so it replaces whatever is on disk.
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.WriteForced("forced replacement");
            File.WriteAllText(path, "external edit");
            return 0;
        });

        Assert.Equal("forced replacement", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(tree.Root));
    }

    [Fact]
    public void Transact_ValidatedWrite_NoExternalChange_CommitsAndLeavesNoBackup()
    {
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(tree.Root));
    }

    [Fact]
    public void Transact_ValidatedWrite_ExternalEditToTheSameContent_IsNotAConflict()
    {
        // The external write holds the same bytes this save commits, so no data is lost.
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("agreed content");
            File.WriteAllText(path, "agreed content");
            return 0;
        });

        Assert.Equal("agreed content", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(tree.Root));
    }

    [Fact]
    public void Transact_ValidatedWrite_ExternalDeleteInTheReplaceWindow_ThrowsConflict()
    {
        // The target is deleted before the replace lands; the save reports a conflict instead of
        // recreating the file.
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        Assert.Throws<AtomicFile.WriteConflictException>(() => AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("my staged replacement");
            File.Delete(path);
            return 0;
        }));

        Assert.False(File.Exists(path)); // the external deletion stands
        Assert.Empty(Directory.GetFiles(tree.Root)); // no leftover temp or backup
    }

    [Fact]
    public void Transact_ValidatedWrite_TargetChangedAgainBetweenReplaceAndRollback_PreservesNewerDataAndReportsUncertain()
    {
        // An external edit lands before the replace, so the backup differs from the snapshot. A
        // second, newer external write then lands before the rollback. Restoring the backup would
        // overwrite that newer data, so the save keeps both files and reports uncertain.
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        Assert.Throws<AtomicFile.WriteUncertainException>(() => AtomicFile.Transact(
            path,
            transaction =>
            {
                transaction.Write("my staged replacement");
                File.WriteAllText(path, "first external edit"); // becomes the displaced backup
                return 0;
            },
            afterReplace: () => File.WriteAllText(path, "second newer external edit")));

        Assert.Equal("second newer external edit", File.ReadAllText(path)); // the newer data stands
        var preserved = Directory.GetFiles(tree.Root)
            .Where(file => !string.Equals(file, path, StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToList();
        Assert.Contains("first external edit", preserved); // the captured backup is preserved, not lost
    }

    [Fact]
    public void Transact_ValidatedWrite_TargetDeletedAgainBetweenReplaceAndRollback_PreservesBackupAndReportsUncertain()
    {
        // The target is deleted between the replace and the rollback. Restoring the backup would
        // bring back a deleted file with older bytes, so the save keeps the backup and reports
        // uncertain.
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        Assert.Throws<AtomicFile.WriteUncertainException>(() => AtomicFile.Transact(
            path,
            transaction =>
            {
                transaction.Write("my staged replacement");
                File.WriteAllText(path, "first external edit");
                return 0;
            },
            afterReplace: () => File.Delete(path)));

        Assert.False(File.Exists(path)); // the deletion stands
        var preserved = Directory.GetFiles(tree.Root).Select(File.ReadAllText).ToList();
        Assert.Contains("first external edit", preserved); // the captured backup is preserved
    }

    [Fact]
    public void Transact_ValidatedWrite_PostCommitVerificationFailure_ReportsUncertain()
    {
        // The swap is clean, but an external write replaces our bytes before the save checks them,
        // so it cannot report success.
        using var tree = new TempTree();
        var path = tree.File("doc.txt", "original");

        Assert.Throws<AtomicFile.WriteUncertainException>(() => AtomicFile.Transact(
            path,
            transaction =>
            {
                transaction.Write("committed"); // no external edit in the window: a clean swap
                return 0;
            },
            afterReplace: () => File.WriteAllText(path, "newer external content")));

        Assert.Equal("newer external content", File.ReadAllText(path)); // the newer data stands
    }

    [Fact]
    public void Transact_WriteThatFailsToStage_PreservesTheOriginalUnchanged()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // directory permission bits do not block file creation the same way on Windows
        }

        using var tree = new TempTree();
        var directory = tree.Dir("locked");
        var path = Path.Combine(directory, "doc.txt");
        File.WriteAllText(path, "original");

        // A read-only directory makes staging the temp file beside the target fail.
        var info = new DirectoryInfo(directory);
        var mode = File.GetUnixFileMode(directory);
        File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            Assert.ThrowsAny<Exception>(() => AtomicFile.Transact(path, transaction =>
            {
                transaction.Write("replacement that must never land");
                return 0;
            }));
            Assert.Equal("original", File.ReadAllText(path));
        }
        finally
        {
            File.SetUnixFileMode(directory, mode);
            _ = info;
        }
    }

    [Fact]
    public void Transact_Write_ThroughAResolvedSymlink_UpdatesTheTargetAndKeepsTheLink()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "old");
        var link = Path.Combine(tree.Root, "link.dialogue.md");
        Support.Symlinks.Create(link, real);

        // The live session resolves the link before writing, so the atomic replace lands on the
        // real file instead of clobbering the link entry with a regular file.
        var resolved = SymlinkResolver.Resolve(link);
        AtomicFile.Transact(resolved, transaction =>
        {
            transaction.Write("new");
            return 0;
        });

        Assert.Equal("new", File.ReadAllText(real)); // the real target was updated in place
        Assert.NotNull(new FileInfo(link).LinkTarget); // the link entry is preserved
        Assert.Equal("new", File.ReadAllText(link)); // and still reads through to the target
    }
}
