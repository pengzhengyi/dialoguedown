using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Configuration;
using DialogueDown.ConfigurationLoader;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// A live session bound to one document. It reads and compiles the current file on
/// demand (for the initial page and the document API) and pushes hot-reload events
/// to connected clients; the watcher calls <see cref="Refresh"/> when the file
/// changes on disk. Saving is optimistic and generation-safe: a write carries the
/// baseline it expects on disk (<see cref="Save"/>), so an external change is reported
/// as a conflict instead of being overwritten silently, and Config Auto validates the
/// TOML before writing. When the compile applied a <c>dialogue.toml</c>, the session
/// can also save an edited configuration and recompile with it; a session with no
/// configuration file can create one (<see cref="CreateConfig"/>).
/// </summary>
internal sealed class LiveSession
{
    private string? _configPath;
    private CompilationVisualizer _visualizer;
    private volatile string? _lastSaved;
    private volatile string? _lastSavedConfig;

    // The configuration currently persisted on disk, tracked apart from the last valid visualizer:
    // a saved-invalid Config keeps the last valid report but persists invalid TOML, so a page
    // reload must restore that saved-invalid state rather than the last valid text.
    private string? _currentConfigSource;
    private bool _currentConfigValid = true;
    private string _currentConfigError = string.Empty;

    /// <summary>Creates a session for <paramref name="documentPath"/> in the given <paramref name="mode"/>.</summary>
    public LiveSession(
        string documentPath,
        string mode = VisualizationMode.View,
        CompilationVisualizer? visualizer = null,
        string? configPath = null,
        string? displayPath = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(mode);
        DocumentPath = documentPath;
        DisplayPath = displayPath ?? documentPath;
        Mode = mode;
        _visualizer = visualizer ?? new CompilationVisualizer();
        _configPath = configPath;
    }

    /// <summary>
    /// The absolute path this session reads, writes, and watches. When the launched path was a
    /// symbolic link this is its resolved real target, so an atomic save replaces the real file
    /// instead of clobbering the link and the watcher sees the real file's changes.
    /// </summary>
    public string DocumentPath { get; }

    /// <summary>
    /// The absolute path shown to the reader (the report's path chrome) — the path the session was
    /// launched with, which may be a symbolic link even though <see cref="DocumentPath"/> is its
    /// resolved target.
    /// </summary>
    public string DisplayPath { get; }

    /// <summary>The mode this session serves in (watch or live).</summary>
    public string Mode { get; }

    /// <summary>
    /// The served-mode project context carried in the report payload so the client renders the
    /// Explorer sidebar — the project root and this document's root-relative path. Null when the
    /// session is not part of a browsable project.
    /// </summary>
    public ReportProject? Project { get; set; }

    /// <summary>
    /// The absolute path of the configuration file this session applies, or <c>null</c> when the
    /// document compiles on the built-in defaults. It becomes non-null once a config is created or
    /// adopted (see <see cref="CreateConfig"/>), so the caller can start watching it for external
    /// changes.
    /// </summary>
    public string? ConfigPath => _configPath;

    /// <summary>The event stream shared by every connected client.</summary>
    public SseBroadcaster Broadcaster { get; } = new();

    /// <summary>Renders the initial live report HTML for the current file.</summary>
    public string RenderInitialHtml() =>
        RenderReportHtml(File.ReadAllText(DocumentPath), CurrentConfigOverlay());

    /// <summary>Serializes the current document payload (<c>{ mode, path, source, stages }</c>).</summary>
    public string CurrentDocumentJson() =>
        SerializeReportDocument(File.ReadAllText(DocumentPath), CurrentConfigOverlay());

    /// <summary>
    /// Applies one save request and returns a payload carrying a typed <c>outcome</c>:
    /// <c>saved</c>, <c>saved-invalid</c>, <c>invalid-auto</c>, <c>conflict</c>, or
    /// <c>uncertain</c> (a write that could not establish a safe state on disk without risking
    /// newer external data). A dialogue
    /// save is always written (its errors surface as diagnostics); a Config save validates when
    /// the request requires it, and every non-forced write first compares the disk against the
    /// requested source (idempotent recovery) and the expected baseline (conflict detection).
    /// Records the written content so the watcher's self-triggered <see cref="Refresh"/> is not
    /// mistaken for an external edit.
    /// </summary>
    public string Save(SaveInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Save(input, afterReplace: null);
    }

    /// <summary>
    /// Reloads the document (or its configuration, when <paramref name="target"/> is
    /// <c>"config"</c>) from disk for a Reload from a conflict/uncertain state, returning a
    /// payload with a typed <c>outcome</c> (<c>loaded</c>, <c>invalid</c>, or <c>missing</c>).
    /// A valid Config reload adopts the external file; an invalid one keeps the last valid
    /// report and carries the external TOML so the editor can show it.
    /// </summary>
    public string Reload(string? target)
    {
        var isConfig = string.Equals(target, "config", StringComparison.OrdinalIgnoreCase);
        return isConfig ? ReloadConfig() : ReloadDocument();
    }

    /// <summary>
    /// Creates a <c>dialogue.toml</c> at <paramref name="configPath"/> for a session that has none
    /// and adopts it (so later saves and reloads apply it). The write is exclusive
    /// (<see cref="FileMode.CreateNew"/>): it either creates the starter file
    /// (<see cref="CreateConfigStatus.Created"/>), or — when a file already exists — adopts it. An
    /// existing file equal to the starter template is adopted idempotently
    /// (<see cref="CreateConfigStatus.Adopted"/>, a create retry after a lost response); a
    /// <em>different</em> pre-existing file is adopted without overwriting it
    /// (<see cref="CreateConfigStatus.AdoptedExisting"/>) — valid TOML into the visualizer, invalid
    /// TOML as saved-invalid — so a config-less session recovers into the existing configuration
    /// rather than a dead end. <see cref="ConfigPath"/> is assigned only after a successful creation
    /// or adoption, so a failed create leaves the no-config state unchanged for a retry. A retry that
    /// arrives after this session already adopted the same <paramref name="configPath"/> (its first
    /// response was lost) is idempotent while the file is still the untouched starter template and a
    /// <see cref="CreateConfigStatus.Conflict"/> once the content diverges (the session already
    /// applies the file, so a reload opens it). Throws <see cref="InvalidOperationException"/> when
    /// the session already has a <em>different</em> configuration file.
    /// </summary>
    public CreateConfigResult CreateConfig(string configPath)
    {
        ArgumentNullException.ThrowIfNull(configPath);
        if (_configPath is not null)
        {
            if (!PathsEqual(configPath, _configPath))
            {
                throw new InvalidOperationException("This session already has a configuration file.");
            }

            // A lost-response retry of the create that this session already satisfied: idempotent
            // while the file is still the starter template, a conflict once its content diverges.
            return AdoptOrConflict(configPath);
        }

        try
        {
            AtomicFile.CreateNew(configPath, ConfigStarter.Template);
        }
        catch (IOException) when (File.Exists(configPath))
        {
            // A dialogue.toml already exists at the serve root: adopt it as recovery rather than
            // leaving the session config-less. An untouched starter template is a lost-create-response
            // adoption; any other content is an existing configuration adopted without overwriting.
            return AdoptExisting(configPath);
        }

        return new CreateConfigResult(
            CreateConfigStatus.Created, AdoptConfig(configPath, ConfigStarter.Template));
    }

    /// <summary>
    /// Recompiles the current file and pushes a <c>reload</c> to every client, or a
    /// <c>problem</c> event carrying an error message when the file is missing or
    /// cannot be read. A change whose content matches the last write is the browser's own write
    /// and is skipped, so a save does not bounce back as a reload.
    /// </summary>
    public void Refresh()
    {
        try
        {
            var current = File.ReadAllText(DocumentPath);
            // One-shot suppression: a self-write arms a single suppression token, consumed here so a
            // later external change back to the same content still reloads (an A->B->A sequence).
            var expected = _lastSaved;
            _lastSaved = null;
            if (current == expected)
            {
                return;
            }

            Broadcaster.Broadcast(
                new LiveEvent("reload", SerializeReportDocument(current)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var message = ex is FileNotFoundException or DirectoryNotFoundException
                ? $"Document not found: {DocumentPath}"
                : ex.Message;
            Broadcaster.Broadcast(new LiveEvent("problem", ProblemJson(message, "document")));
        }
    }

    /// <summary>
    /// Recompiles with the configuration's current on-disk content and pushes a
    /// <c>reload-config</c> to every client (a <c>problem</c> event when the file is missing or
    /// cannot be read). A valid external config is adopted so View stays consistent for later
    /// recompiles; an invalid one keeps the last valid report but carries the external TOML.
    /// A change whose content matches the last config write is the browser's own write and is
    /// skipped, so a config save does not bounce back as a reload. Does nothing when the session
    /// has no configuration file.
    /// </summary>
    public void RefreshConfig()
    {
        if (_configPath is null)
        {
            return;
        }

        try
        {
            var current = File.ReadAllText(_configPath);
            // One-shot suppression: a self-write arms a single suppression token, consumed here so a
            // later external change back to the same content still reloads (an A->B->A sequence).
            var expected = _lastSavedConfig;
            _lastSavedConfig = null;
            if (current == expected)
            {
                return;
            }

            Broadcaster.Broadcast(new LiveEvent("reload-config", ConfigReloadPayload(current)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var message = ex is FileNotFoundException or DirectoryNotFoundException
                ? $"Configuration not found: {_configPath}"
                : ex.Message;
            Broadcaster.Broadcast(new LiveEvent("problem", ProblemJson(message, "config")));
        }
    }

    // A test seam mirroring <see cref="AtomicFile"/>'s: <paramref name="afterReplace"/> is threaded
    // into the atomic replace so a test can deterministically drive the target-changed-again and
    // post-commit-verification races that make a write uncertain.
    internal string Save(SaveInput input, Action? afterReplace)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.IsConfig ? SaveConfig(input, afterReplace) : SaveDocument(input, afterReplace);
    }

    private static string WithOutcome(string payloadJson, string outcome)
    {
        var node = JsonNode.Parse(payloadJson)!.AsObject();
        node["outcome"] = outcome;
        return node.ToJsonString();
    }

    // A saved-invalid/invalid Config payload keeps the last valid report but must carry the
    // external (invalid) TOML and a configuration file the Config tab can open. Serializing with a
    private static string OutcomeJson(string outcome, string message) =>
        JsonSerializer.Serialize(new { outcome, message });

    // A missing-file/read-failure problem carries which document it is about (the served
    // document or its configuration) so the client can route it through that controller's
    // disk-change/conflict path instead of only flashing a banner.
    private static string ProblemJson(string message, string target) =>
        JsonSerializer.Serialize(new { message, target });

    // Two paths name the same file — compared as normalized full paths so a session recognizes a
    // create retry for the very config it already adopted.
    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.Ordinal);

    // A saved-invalid/invalid Config payload keeps the last valid report but must carry the
    // external (invalid) TOML and a configuration file the Config tab can open. Serializing with a
    // ConfigStatusOverlay guarantees a configuration.file (path and invalid source) plus the
    // saved-invalid status/message even when no valid configuration was ever applied; the outcome
    // and message are then layered on for the save/reload state machine.
    private string InvalidConfigPayload(string source, string error, string outcome)
    {
        var overlay = new ConfigStatusOverlay(_configPath!, source, error);
        var json = SerializeReportDocument(File.ReadAllText(DocumentPath), overlay);
        var node = JsonNode.Parse(json)!.AsObject();
        node["outcome"] = outcome;
        node["message"] = error;
        return node.ToJsonString();
    }

    // A create retry for the config this session already adopted lost the race to the existing file:
    // adopt it idempotently when it is still the starter template, otherwise report a conflict (the
    // session already applies the file, so a reload opens it) and leave it untouched.
    private CreateConfigResult AdoptOrConflict(string configPath)
    {
        var existing = File.ReadAllText(configPath);
        if (!string.Equals(existing, ConfigStarter.Template, StringComparison.Ordinal))
        {
            return new CreateConfigResult(
                CreateConfigStatus.Conflict, "A dialogue.toml already exists — reload to edit it.");
        }

        return new CreateConfigResult(CreateConfigStatus.Adopted, AdoptConfig(configPath, existing));
    }

    // A config-less session's create lost the race to a pre-existing file: adopt it without
    // overwriting. An untouched starter template is a lost-create-response adoption; any other
    // content is an existing configuration adopted as recovery (valid, or saved-invalid) so the
    // session is no longer config-less and the frontend can reload and open it.
    private CreateConfigResult AdoptExisting(string configPath)
    {
        var existing = File.ReadAllText(configPath);
        if (string.Equals(existing, ConfigStarter.Template, StringComparison.Ordinal))
        {
            return new CreateConfigResult(CreateConfigStatus.Adopted, AdoptConfig(configPath, existing));
        }

        return new CreateConfigResult(
            CreateConfigStatus.AdoptedExisting, AdoptExistingConfig(configPath, existing));
    }

    // Parses and adopts an on-disk config (already written) without rewriting it, assigning
    // _configPath only after the parse succeeds and recording it as the last self-write so the
    // config watcher's own event is suppressed. Returns the recompiled document payload.
    private string AdoptConfig(string configPath, string source)
    {
        var options = TomlConfigurationLoader.Parse(source, configPath);
        _configPath = configPath;
        AdoptValidConfig(source, options);
        _lastSavedConfig = source;
        return WithOutcome(SerializeCurrent(), "saved");
    }

    // Adopts a differing pre-existing config without rewriting it: valid TOML is adopted into the
    // visualizer and returned as `adopted`; invalid TOML keeps the last valid report and is recorded
    // as saved-invalid, returned as `adopted-invalid` carrying the (invalid) source so a reloaded
    // page can open it. _configPath is assigned so the session is no longer config-less.
    private string AdoptExistingConfig(string configPath, string source)
    {
        _configPath = configPath;
        _lastSavedConfig = source;
        if (TryParseConfig(source, out var options, out var error))
        {
            AdoptValidConfig(source, options!);
            return WithOutcome(SerializeCurrent(), "adopted");
        }

        RecordInvalidConfig(source, error);
        return InvalidConfigPayload(source, error, "adopted-invalid");
    }

    // Builds the payload for an external config change: a valid config is adopted into the
    // visualizer and returned as `loaded`; an invalid one keeps the last valid report and carries
    // the external (invalid) TOML as `invalid` so the editor can show it.
    private string ConfigReloadPayload(string source)
    {
        if (TryParseConfig(source, out var options, out var error))
        {
            AdoptValidConfig(source, options!);
            return WithOutcome(SerializeCurrent(), "loaded");
        }

        RecordInvalidConfig(source, error);
        return InvalidConfigPayload(source, error, "invalid");
    }

    // Adopts a valid config into the visualizer and records it as the current, valid on-disk
    // configuration so a page reload renders it without a stale overlay.
    private void AdoptValidConfig(string source, CompilerOptions options)
    {
        _visualizer = new CompilationVisualizer(
            AppliedConfiguration.FromFile(_configPath!, source, options));
        _currentConfigSource = source;
        _currentConfigValid = true;
        _currentConfigError = string.Empty;
    }

    // Records the current on-disk config as persisted-but-invalid so a page reload restores the
    // saved-invalid state (the invalid source and a stale report) instead of the last valid text.
    private void RecordInvalidConfig(string source, string error)
    {
        _currentConfigSource = source;
        _currentConfigValid = false;
        _currentConfigError = error;
    }

    // The overlay a served page needs when the persisted config is invalid: none while it is valid.
    private ConfigStatusOverlay? CurrentConfigOverlay() =>
        _currentConfigValid
            ? null
            : new ConfigStatusOverlay(
                _configPath ?? string.Empty, _currentConfigSource ?? string.Empty, _currentConfigError);

    private string SaveDocument(SaveInput input, Action? afterReplace)
    {
        var source = input.Source ?? string.Empty;
        try
        {
            return AtomicFile.Transact(
                DocumentPath,
                transaction =>
                {
                    var disk = transaction.Disk;
                    if (!input.Overwrite)
                    {
                        if (disk == source)
                        {
                            _lastSaved = source; // idempotent: the disk already equals the requested source
                            return WithOutcome(SerializeReportDocument(source), "saved");
                        }

                        if (disk != input.ExpectedBaseline)
                        {
                            return OutcomeJson("conflict", "The document changed on disk.");
                        }

                        _lastSaved = source;
                        transaction.Write(source); // compare-and-swap: an external write in the window conflicts
                    }
                    else
                    {
                        _lastSaved = source;
                        transaction.WriteForced(source); // confirmed overwrite
                    }

                    return WithOutcome(SerializeReportDocument(source), "saved");
                },
                afterReplace);
        }
        catch (AtomicFile.WriteConflictException)
        {
            return OutcomeJson("conflict", "The document changed on disk.");
        }
        catch (AtomicFile.WriteUncertainException)
        {
            return OutcomeJson("uncertain", "The document's state on disk is uncertain; reload or confirm overwrite.");
        }
    }

    private string SaveConfig(SaveInput input, Action? afterReplace)
    {
        if (_configPath is null)
        {
            throw new InvalidOperationException("This session has no configuration file to save.");
        }

        var source = input.Source ?? string.Empty;
        ConfigCommit? committed = null;
        string? immediate;
        try
        {
            immediate = AtomicFile.Transact(
                _configPath,
                transaction =>
                {
                    var disk = transaction.Disk;
                    if (!input.Overwrite)
                    {
                        if (disk == source)
                        {
                            committed = StageConfig(source, transaction, write: false, force: false); // idempotent recovery
                            return null;
                        }

                        if (disk != input.ExpectedBaseline)
                        {
                            return OutcomeJson("conflict", "The configuration changed on disk.");
                        }
                    }

                    if (input.RequireValid && !TryParseConfig(source, out _, out var validationError))
                    {
                        // Auto/navigation never writes invalid TOML; the exclusive handle is released unwritten.
                        return OutcomeJson("invalid-auto", validationError);
                    }

                    committed = StageConfig(source, transaction, write: true, force: input.Overwrite);
                    return null;
                },
                afterReplace);
        }
        catch (AtomicFile.WriteConflictException)
        {
            return OutcomeJson("conflict", "The configuration changed on disk.");
        }
        catch (AtomicFile.WriteUncertainException)
        {
            return OutcomeJson(
                "uncertain", "The configuration's state on disk is uncertain; reload or confirm overwrite.");
        }

        // Publish the recompiled visualizer and config source/validity only now that AtomicFile has
        // confirmed the write committed (or that there was nothing to write): a staging failure or a
        // conflict throws above, so the session's rendered state never advances past disk.
        return immediate ?? PublishConfig(committed!);
    }

    // Parses source into a candidate compile and stages the write through the held transaction (a
    // compare-and-swap, or a confirmed overwrite), without publishing any session state. The
    // candidate is published by PublishConfig only after AtomicFile confirms the commit.
    private ConfigCommit StageConfig(string source, AtomicFile.Transaction transaction, bool write, bool force)
    {
        if (write)
        {
            if (force)
            {
                transaction.WriteForced(source); // confirmed overwrite
            }
            else
            {
                transaction.Write(source); // compare-and-swap against the snapshot
            }
        }

        var valid = TryParseConfig(source, out var options, out var error);
        return new ConfigCommit(source, options, error, valid);
    }

    // Publishes a committed config candidate: valid TOML is adopted into the visualizer and returned
    // as `saved`; invalid TOML keeps the last valid report and returns `saved-invalid` carrying the
    // persisted (invalid) source so the editor can show it. The source is now on disk, so it is
    // recorded as the last self-write to suppress the config watcher's own event (see RefreshConfig).
    private string PublishConfig(ConfigCommit committed)
    {
        _lastSavedConfig = committed.Source;
        if (committed.Valid)
        {
            AdoptValidConfig(committed.Source, committed.Options!);
            return WithOutcome(SerializeCurrent(), "saved");
        }

        RecordInvalidConfig(committed.Source, committed.Error);
        return InvalidConfigPayload(committed.Source, committed.Error, "saved-invalid");
    }

    private string ReloadDocument()
    {
        if (!File.Exists(DocumentPath))
        {
            return OutcomeJson("missing", "The document was deleted on disk.");
        }

        var current = File.ReadAllText(DocumentPath);
        _lastSaved = current;
        return WithOutcome(SerializeReportDocument(current), "loaded");
    }

    private string ReloadConfig()
    {
        if (_configPath is null)
        {
            throw new InvalidOperationException("This session has no configuration file to reload.");
        }

        if (!File.Exists(_configPath))
        {
            return OutcomeJson("missing", "The configuration was deleted on disk.");
        }

        var source = File.ReadAllText(_configPath);
        _lastSavedConfig = source;
        if (TryParseConfig(source, out var options, out var error))
        {
            AdoptValidConfig(source, options!);
            return WithOutcome(SerializeCurrent(), "loaded");
        }

        RecordInvalidConfig(source, error);
        return InvalidConfigPayload(source, error, "invalid");
    }

    // Render or serialize the payload through the visualizer with this session's constants — its
    // display path, mode, and project context — so every payload it emits carries the Explorer's
    // project for the client to render the sidebar.
    private string RenderReportHtml(string source, ConfigStatusOverlay? overlay = null) =>
        _visualizer.RenderLiveReport(DisplayPath, source, Mode, overlay, Project);

    private string SerializeReportDocument(string source, ConfigStatusOverlay? overlay = null) =>
        _visualizer.SerializeDocument(DisplayPath, source, Mode, overlay, Project);

    private string SerializeCurrent() =>
        SerializeReportDocument(File.ReadAllText(DocumentPath));

    private bool TryParseConfig(string source, out CompilerOptions? options, out string error)
    {
        try
        {
            options = TomlConfigurationLoader.Parse(source, _configPath!);
            error = string.Empty;
            return true;
        }
        catch (DialogueConfigurationException ex)
        {
            options = null;
            error = ex.Message;
            return false;
        }
    }

    // A parsed configuration candidate staged for commit: its source, the parsed options (when
    // valid), the parse error (when invalid), and whether it parsed. Published only post-commit.
    private sealed record ConfigCommit(string Source, CompilerOptions? Options, string Error, bool Valid);
}
