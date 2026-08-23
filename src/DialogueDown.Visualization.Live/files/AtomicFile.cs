using System.Text;

namespace DialogueDown.Visualization.Live.Files;

/// <summary>How a staged write is published on commit.</summary>
internal enum WriteMode
{
    /// <summary>Nothing was staged; commit touches nothing.</summary>
    None,

    /// <summary>A compare-and-swap against the snapshot; an external write in the window is a conflict.</summary>
    Validated,

    /// <summary>An unconditional overwrite of whatever is on disk (a confirmed overwrite).</summary>
    Forced,
}

/// <summary>
/// A read → decide → write window over one file that never destroys data. A caller reads the
/// file's current content (a snapshot taken by an atomic open, so a missing file is reported as
/// <c>null</c> without a stale <see cref="File.Exists"/> probe), runs its baseline/validation
/// checks, and optionally stages a full replacement. A staged write is buffered in full, written
/// to a same-directory temporary file, flushed to disk, and then moved atomically over the target,
/// so a failure at any point leaves the original file byte-for-byte intact and never creates or
/// deletes an externally owned file.
/// </summary>
/// <remarks>
/// Writes to the same path are serialized within this process by a per-path lock, so two
/// concurrent in-process saves from the same baseline settle as one write and one conflict rather
/// than a lost update. Against a <em>separate</em> process (an external editor) this is
/// <em>optimistic</em> concurrency, not a held OS lock: the snapshot read and the atomic replace
/// are distinct steps, which keeps the replace a portable, cross-volume-safe rename. A validated
/// <see cref="Transaction.Write"/> closes that window as a compare-and-swap — the atomic replace
/// captures the target's immediately previous content into a backup and, if it differs from the
/// snapshot the caller decided on, rolls the external content back and reports a conflict — so an
/// edit that lands between the snapshot and the replace is never silently overwritten. The atomic
/// move also guarantees a reader ever sees only the whole old file or the whole new file — never a
/// partial write.
/// </remarks>
internal static class AtomicFile
{
    // A brief bounded spin lets a concurrent writer (another save, or a quick external editor)
    // release its handle so the atomic replace can complete instead of failing the save outright.
    //
    // The spin sleeps the thread rather than awaiting, and stays that way deliberately. Renaming
    // has no asynchronous form to call: POSIX `rename`/`unlink` and Win32 `MoveFileEx`/`ReplaceFile`
    // are blocking, unlike read and write, so the BCL offers `ReadAllTextAsync` but no `MoveAsync`.
    // The libraries that appear to solve this elsewhere — `aiofiles`, `fs.promises` — do not make
    // the call asynchronous either; they run the same blocking call on a thread pool, which is what
    // `Task.Run` would do here and what .NET guidance tells a library not to hide behind an async
    // signature. Awaiting only the delay is possible, but it would cost the monitor below — `await`
    // cannot cross a `lock` — and the publish itself would block regardless. The retry is reached
    // about never in practice, and this server has one user, so the thread it holds is not scarce.
    private const int MaxAttempts = 100;

    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(5);

    // One lock per target path serializes this process's own read→decide→write windows, so two
    // concurrent in-process saves from the same baseline settle as one write and one conflict
    // rather than a lost update. It does not serialize a separate external process — that narrower,
    // portable guarantee is covered by the baseline check and the watcher (see the type remarks).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _locks =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Reads the current content of <paramref name="path"/> and invokes <paramref name="body"/>
    /// with a <see cref="Transaction"/> exposing that content and a <see cref="Transaction.Write"/>
    /// that stages a full replacement. A file that does not exist is reported as <c>null</c>
    /// content; a no-write outcome touches nothing on disk, so a file that was never present stays
    /// absent and an externally created one is left untouched. A staged <see cref="Transaction.Write"/>
    /// is a compare-and-swap: after the atomic replace it validates the content it displaced against
    /// the snapshot the body decided on, and if an external process wrote the target in the
    /// meantime it rolls the external content back into place and throws
    /// <see cref="WriteConflictException"/> rather than silently overwriting that edit. A staged
    /// <see cref="Transaction.WriteForced"/> replaces whatever is on disk unconditionally (a
    /// confirmed overwrite). Either way the original is never truncated in place and a failed write
    /// preserves it.
    /// </summary>
    /// <exception cref="WriteConflictException">
    /// A validated <see cref="Transaction.Write"/> found the target changed by another process
    /// between the snapshot and the replace; the external content is left on disk.
    /// </exception>
    public static T Transact<T>(string path, Func<Transaction, T> body)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(body);

        return Transact(path, body, afterReplace: null);
    }

    /// <summary>
    /// Creates <paramref name="path"/> from <paramref name="content"/> only when it does not
    /// already exist: the full bytes are staged in a same-directory temporary file, flushed, then
    /// moved into place with a no-overwrite atomic move, so a concurrent create loses the race and
    /// an incomplete temp is removed on any failure. Throws <see cref="IOException"/> when a file
    /// already exists at <paramref name="path"/> (the target is left untouched), matching the
    /// exclusive-create contract callers detect a conflict by.
    /// </summary>
    public static void CreateNew(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        lock (LockFor(fullPath))
        {
            try
            {
                using (var stream = new FileStream(
                    temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, _utf8NoBom, leaveOpen: true))
                    {
                        writer.Write(content);
                        writer.Flush();
                    }

                    stream.Flush(flushToDisk: true);
                }

                // No-overwrite: a file already at the target makes this throw, so the existing file is
                // never clobbered and the caller recognizes the conflict.
                File.Move(temp, fullPath, overwrite: false);
            }
            catch
            {
                TryDelete(temp);
                throw;
            }
        }
    }

    // A test seam: <paramref name="afterReplace"/> runs inside a validated replace, after the
    // atomic swap captured the displaced backup but before the rollback/verification reads the
    // target, so a test can deterministically reproduce a target changed or deleted again in that
    // window (or a post-commit overwrite) without racing real threads. Threaded through the call
    // rather than held in static state so parallel test collections never see each other's hook.
    internal static T Transact<T>(string path, Func<Transaction, T> body, Action? afterReplace)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(body);

        lock (LockFor(path))
        {
            var disk = ReadSnapshot(path);
            var transaction = new Transaction(disk);
            var result = body(transaction);

            switch (transaction.Mode)
            {
                case WriteMode.Validated:
                    Commit(path, transaction.PendingContent, expected: disk, validate: true, afterReplace);
                    break;
                case WriteMode.Forced:
                    Commit(path, transaction.PendingContent, expected: null, validate: false, afterReplace: null);
                    break;
            }

            return result;
        }
    }

    private static object LockFor(string path) =>
        _locks.GetOrAdd(Path.GetFullPath(path), _ => new object());

    // Reads the file's content, or null when it does not exist. Existence is established by the
    // open itself (a FileNotFound/DirectoryNotFound throw) rather than a separate File.Exists probe
    // that could race a concurrent create or delete.
    private static string? ReadSnapshot(string path)
    {
        try
        {
            using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(
                stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    // Stages the complete replacement bytes in a same-directory temporary file, flushes them to
    // disk, then publishes them over the target. A forced commit moves the temp over whatever is
    // there. A validated commit is a compare-and-swap: a create (expected null) uses a no-overwrite
    // move so an external create wins the race; a replace captures the target's immediately previous
    // content into a backup and, if that content is neither the expected snapshot nor the bytes just
    // written, an external process wrote in the window — the backup is restored and a conflict is
    // reported. The original is untouched until the publish, so any staging failure leaves it
    // intact, and the temp/backup are always cleaned.
    private static void Commit(string path, string content, string? expected, bool validate, Action? afterReplace)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            StageTemp(temp, content);

            if (!validate)
            {
                Replace(temp, fullPath);
            }
            else if (expected is null)
            {
                CreateNoOverwrite(temp, fullPath);
            }
            else
            {
                ReplaceValidated(temp, fullPath, expected, content, afterReplace);
            }
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    // Writes the full replacement bytes to a same-directory temp file and flushes them to disk, so a
    // later move is a whole-file publish and a same-volume rename rather than a cross-device copy.
    private static void StageTemp(string temp, string content)
    {
        using var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using (var writer = new StreamWriter(stream, _utf8NoBom, leaveOpen: true))
        {
            writer.Write(content);
            writer.Flush();
        }

        stream.Flush(flushToDisk: true);
    }

    // A create's publish: the snapshot reported the file absent, so a no-overwrite move keeps an
    // external process that created it first from being clobbered — that race is a conflict.
    private static void CreateNoOverwrite(string temp, string target)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temp, target, overwrite: false);
                return;
            }
            catch (IOException) when (File.Exists(target))
            {
                TryDelete(temp);
                throw new WriteConflictException();
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(_retryDelay);
            }
        }
    }

    // A validated replace's publish: atomically swap the temp over the target while capturing the
    // target's immediately previous content into a backup. If that captured content is neither the
    // expected snapshot nor the bytes just written, an external process wrote in the window; the
    // external content is rolled back only when the target still holds the bytes this write just
    // published — otherwise a still newer external write (or another deletion) landed between the
    // replace and the rollback, so the newer data is preserved and the outcome is uncertain rather
    // than a lost update. A clean swap is confirmed by a post-commit read of the target; if that
    // read no longer sees the published bytes, an external overwrite raced the commit and the
    // outcome is likewise uncertain. Either uncertain path keeps the captured backup on disk so no
    // content is silently deleted. A clean, verified swap removes the backup.
    private static void ReplaceValidated(
        string temp, string target, string expected, string content, Action? afterReplace)
    {
        var backup = target + $".{Guid.NewGuid():N}.bak";
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Replace(temp, target, backup, ignoreMetadataErrors: true);
                break;
            }
            catch (FileNotFoundException)
            {
                // The target was deleted out from under the replace: an external change, not a swap.
                TryDelete(temp);
                TryDelete(backup);
                throw new WriteConflictException();
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(_retryDelay);
            }
        }

        var displaced = ReadSnapshot(backup);
        afterReplace?.Invoke();

        if (displaced == expected || displaced == content)
        {
            // A clean swap. Verify the target still holds the bytes we published before declaring
            // success: an external overwrite that raced the commit leaves newer data we must not mask.
            if (ReadSnapshot(target) == content)
            {
                TryDelete(backup);
                return;
            }

            // Post-commit verification failed: newer external data is on the target. Preserve the
            // captured backup and surface an uncertain outcome instead of a false success.
            throw new WriteUncertainException();
        }

        // An external edit landed in the window. Roll it back into place only when the target still
        // holds the bytes we just published; otherwise a still newer external write (or a deletion)
        // arrived after the replace, and overwriting it with the older backup would lose data.
        if (ReadSnapshot(target) == content)
        {
            Replace(backup, target);
            throw new WriteConflictException();
        }

        // The target changed or was deleted again between the replace and the rollback: keep the
        // captured backup on disk and report uncertain rather than clobber the newer external data.
        throw new WriteUncertainException();
    }

    // Atomically moves the staged temp over the target, retrying briefly so a transient handle held
    // by another writer does not fail the save. Replacing an existing file and creating a new one
    // are one operation (overwrite), so the target's existence never has to be probed first.
    private static void Replace(string temp, string target)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temp, target, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(_retryDelay);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best effort: a leftover uniquely named temp is harmless and never the target file.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort: see above.
        }
    }

    /// <summary>
    /// One read → decide → write transaction: the <see cref="Disk"/> content snapshot, a
    /// <see cref="Write"/> that stages a full replacement committed as a compare-and-swap after the
    /// body returns, and a <see cref="WriteForced"/> that stages an unconditional overwrite.
    /// </summary>
    internal sealed class Transaction
    {
        internal Transaction(string? disk) => Disk = disk;

        /// <summary>The file's content when the transaction began, or <c>null</c> if it did not exist.</summary>
        public string? Disk { get; }

        /// <summary>How this transaction's staged content (if any) is published on commit.</summary>
        internal WriteMode Mode { get; private set; } = WriteMode.None;

        /// <summary>The staged replacement content, valid only when <see cref="Mode"/> is not <see cref="WriteMode.None"/>.</summary>
        internal string PendingContent { get; private set; } = string.Empty;

        /// <summary>
        /// Stages <paramref name="content"/> as the file's full replacement, committed on return as
        /// a compare-and-swap against <see cref="Disk"/>: an external write that lands in the
        /// replace window is detected and reported as a <see cref="WriteConflictException"/> instead
        /// of being overwritten.
        /// </summary>
        public void Write(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            PendingContent = content;
            Mode = WriteMode.Validated;
        }

        /// <summary>
        /// Stages <paramref name="content"/> as an unconditional overwrite — a confirmed overwrite
        /// that intentionally replaces whatever is on disk, still atomically and non-destructively.
        /// </summary>
        public void WriteForced(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            PendingContent = content;
            Mode = WriteMode.Forced;
        }
    }

    /// <summary>
    /// Signals that a validated <see cref="Transaction.Write"/> found the target changed by another
    /// process between the snapshot and the atomic replace. The external content is left on disk
    /// (rolled back into place if the swap already happened), and the caller reports a conflict
    /// rather than overwriting the edit.
    /// </summary>
    internal sealed class WriteConflictException : Exception
    {
        public WriteConflictException()
            : base("The file changed on disk during the write.")
        {
        }
    }

    /// <summary>
    /// Signals that a validated <see cref="Transaction.Write"/> could not establish a safe final
    /// state: the target changed or was deleted again between the atomic replace and the rollback,
    /// or a post-commit verification found the target no longer held the published bytes. Newer
    /// external data is left in place and the captured backup is preserved on disk, so no content
    /// is lost; the caller must reconcile (reload or a confirmed overwrite) rather than treat this
    /// as an ordinary no-write failure.
    /// </summary>
    internal sealed class WriteUncertainException : Exception
    {
        public WriteUncertainException()
            : base("The file could not be safely written; its state on disk is uncertain.")
        {
        }
    }
}
