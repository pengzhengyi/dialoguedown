using System.Text;
using DialogueDown.Visualization.Live.Files;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class AtomicFileTests
{
    [Fact]
    public void Transact_ReadsTheCurrentContentUnderTheLock()
    {
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "on disk");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Equal("on disk", seen);
    }

    [Fact]
    public void Transact_MissingFile_ReportsNullAndLeavesNoFileWhenNotWritten()
    {
        using var tree = new Support.TempTree();
        var path = Path.Combine(tree.Root, "missing.txt");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Null(seen);
        Assert.False(File.Exists(path)); // the inspection placeholder is removed on a no-write outcome
    }

    [Fact]
    public void Transact_Write_CommitsContentWithoutABom()
    {
        using var tree = new Support.TempTree();
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
        using var tree = new Support.TempTree();
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
        // The containing directory does not exist: the snapshot read reports a missing file (null)
        // rather than throwing, and a no-write body leaves nothing behind.
        using var tree = new Support.TempTree();
        var path = Path.Combine(tree.Root, "nope", "doc.txt");

        var seen = AtomicFile.Transact(path, transaction => transaction.Disk);

        Assert.Null(seen);
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public void Transact_MissingFileCreatedConcurrently_IsReadAndNeverDeleted()
    {
        // Simulate the existence race: the target does not exist when the transaction begins, but
        // an external writer creates it while the body runs. A no-write outcome must never delete
        // that externally created file (the old File.Exists placeholder-and-delete bug).
        using var tree = new Support.TempTree();
        var path = Path.Combine(tree.Root, "raced.txt");

        var seen = AtomicFile.Transact(path, transaction =>
        {
            File.WriteAllText(path, "external content"); // appears mid-transaction
            return transaction.Disk;
        });

        Assert.Null(seen); // it did not exist when the snapshot was read
        Assert.True(File.Exists(path));
        Assert.Equal("external content", File.ReadAllText(path)); // never deleted or clobbered
    }

    [Fact]
    public void Transact_Write_LeavesNoStagingTempBehind()
    {
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "original");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        // The staged bytes move atomically into place; no leftover temp file remains beside it.
        Assert.Single(Directory.GetFiles(tree.Root));
    }

    [Fact]
    public void Transact_Write_StagesInTheSameDirectory()
    {
        // The staging temp must be a sibling of the target so the final move stays on one volume
        // (an atomic rename), not a cross-device copy through the system temp folder.
        using var tree = new Support.TempTree();
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
        // The cross-process race: the snapshot is read, the caller stages a full replacement, but an
        // external editor writes the target before the atomic replace lands. The staged write must
        // not silently clobber that external edit — the commit captures the immediately previous
        // target into a backup, sees it differs from the expected baseline, rolls back, and reports
        // a conflict. Writing to the path inside the body reproduces the window because the commit
        // runs after the body returns.
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "original");

        Assert.Throws<AtomicFile.WriteConflictException>(() => AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("my staged replacement");
            File.WriteAllText(path, "external edit"); // lands in the pre-replace window
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
        using var tree = new Support.TempTree();
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
        // A confirmed overwrite is force: it intentionally replaces whatever is on disk, so it
        // commits over an external edit rather than reporting a conflict.
        using var tree = new Support.TempTree();
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
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "original");

        AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("committed");
            return 0;
        });

        Assert.Equal("committed", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(tree.Root)); // the backup is removed after a clean commit
    }

    [Fact]
    public void Transact_ValidatedWrite_ExternalEditToTheSameContent_IsNotAConflict()
    {
        // The external write in the window produced the very bytes this save is committing: no data
        // is lost, so it settles as a normal commit rather than a spurious conflict.
        using var tree = new Support.TempTree();
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
        // The target is deleted out from under the atomic replace (an external change, not a swap):
        // the write must report a conflict rather than recreate the file the deleter intended gone.
        using var tree = new Support.TempTree();
        var path = tree.File("doc.txt", "original");

        Assert.Throws<AtomicFile.WriteConflictException>(() => AtomicFile.Transact(path, transaction =>
        {
            transaction.Write("my staged replacement");
            File.Delete(path); // vanishes before the replace lands
            return 0;
        }));

        Assert.False(File.Exists(path)); // the external deletion stands
        Assert.Empty(Directory.GetFiles(tree.Root)); // no leftover temp or backup
    }

    [Fact]
    public void Transact_ValidatedWrite_TargetChangedAgainBetweenReplaceAndRollback_PreservesNewerDataAndReportsUncertain()
    {
        // The conflict rollback must never clobber an even newer external write. An external edit
        // lands in the replace window (the displaced backup differs from the baseline), but before
        // the rollback restores it a *second*, newer external write lands on the target. Blindly
        // restoring the first backup would overwrite that newer data, so the write must instead
        // preserve the newer target, keep the captured backup safely, and report an uncertain
        // outcome rather than a plain conflict.
        using var tree = new Support.TempTree();
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
        // recreate a file the deleter intended gone using stale bytes, so the write preserves the
        // captured backup and reports uncertain rather than a plain conflict or a lost update.
        using var tree = new Support.TempTree();
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

        Assert.False(File.Exists(path)); // the deletion stands; no stale resurrection
        var preserved = Directory.GetFiles(tree.Root).Select(File.ReadAllText).ToList();
        Assert.Contains("first external edit", preserved); // the captured backup is preserved
    }

    [Fact]
    public void Transact_ValidatedWrite_PostCommitVerificationFailure_ReportsUncertain()
    {
        // A clean swap committed our bytes, but post-commit verification finds the target no longer
        // holds them: an external process overwrote the file immediately after the swap. The write
        // cannot claim success without possibly masking that newer data, so it reports uncertain
        // and leaves the newer external content in place.
        using var tree = new Support.TempTree();
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

        using var tree = new Support.TempTree();
        var directory = tree.Dir("locked");
        var path = Path.Combine(directory, "doc.txt");
        File.WriteAllText(path, "original");

        // Make the directory read-only so staging a sibling temp file fails; the original must be
        // left byte-for-byte intact rather than truncated or partially written.
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
        using var tree = new Support.TempTree();
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
