using System.Text.Json;
using DialogueDown.ConfigurationLoader;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Tests.Support;
using DialogueDown.Visualization.Render;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LiveSessionTests
{
    [Fact]
    public void RenderInitialHtml_MarksThePayloadWithTheSessionMode()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path);

        var html = session.RenderInitialHtml();

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"mode\":\"view\"", html); // view is the default session mode
    }

    [Fact]
    public void Mode_Explicit_IsCarriedIntoThePayload()
    {
        using var doc = new TempDocument("# Scene");

        var session = new LiveSession(doc.Path, "edit");

        Assert.Equal("edit", session.Mode);
        Assert.Contains("\"mode\":\"edit\"", session.CurrentDocumentJson());
    }

    [Fact]
    public void CurrentDocumentJson_CarriesPathSourceAndStages()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path);

        var json = session.CurrentDocumentJson();

        Assert.Contains("\"path\":", json);
        Assert.Contains("\"source\":\"# Scene\"", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void Refresh_BroadcastsAReloadWithTheCurrentContent()
    {
        using var doc = new TempDocument("# First");
        var session = new LiveSession(doc.Path);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.WriteAllText(doc.Path, "# Second");

        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
        Assert.Contains("# Second", received.Data);
    }

    [Fact]
    public void Refresh_MissingDocument_BroadcastsAProblem()
    {
        var session = new LiveSession(Path.Combine(Path.GetTempPath(), "missing-edit.dialogue.md"));
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("not found", received.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"target\":\"document\"", received.Data); // routes through the document controller
    }

    [Fact]
    public void Refresh_UnreadableDocument_BroadcastsAProblemInsteadOfThrowing()
    {
        // The watcher fires Refresh from a timer callback; an unreadable file (permission denied,
        // or the path became a directory) throws UnauthorizedAccessException, not IOException. It
        // must be caught and broadcast as a targeted problem rather than escaping the callback.
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.Delete(docPath);
        Directory.CreateDirectory(docPath); // reading a directory throws UnauthorizedAccessException

        var problem = Record.Exception(() => session.Refresh());

        Assert.Null(problem); // the callback never escapes
        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("\"target\":\"document\"", received.Data);
    }

    [Fact]
    public void Save_MatchingBaseline_WritesTheBufferAndReturnsSaved()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save(new SaveInput("# New\n\nAlice: Hi", ExpectedBaseline: "# Old"));

        Assert.Equal("# New\n\nAlice: Hi", File.ReadAllText(doc.Path));
        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Contains("\"source\":\"# New", json);
        Assert.Contains("\"stages\":[", json);
    }

    [Fact]
    public void RenderInitialHtml_ThroughASymlink_ShowsTheLaunchedPathNotTheResolvedTarget()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# Scene");
        var link = Path.Combine(tree.Root, "link.dialogue.md");
        Symlinks.Create(link, real);
        var resolved = SymlinkResolver.Resolve(link);

        var session = new LiveSession(resolved, displayPath: link);
        var html = session.RenderInitialHtml();

        Assert.Contains("link.dialogue.md", html); // the report shows the launched link path
        Assert.DoesNotContain("real.dialogue.md", html); // not the resolved real target
    }

    [Fact]
    public void Save_ThroughASymlink_WritesTheRealTargetAndKeepsTheLink()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# Old");
        var link = Path.Combine(tree.Root, "link.dialogue.md");
        Symlinks.Create(link, real);
        var resolved = SymlinkResolver.Resolve(link);

        // A served session resolves the link for IO but keeps the launched link path for display.
        var session = new LiveSession(resolved, "edit", displayPath: link);
        var json = session.Save(new SaveInput("# New\n\nAlice: Hi", ExpectedBaseline: "# Old"));

        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Equal("# New\n\nAlice: Hi", File.ReadAllText(real)); // the real target was written
        Assert.NotNull(new FileInfo(link).LinkTarget); // the link entry is preserved
        Assert.Contains("link.dialogue.md", json); // the payload shows the launched link path
        Assert.DoesNotContain("real.dialogue.md", json); // not the resolved real target
    }

    [Fact]
    public void Save_BaselineMismatch_ReturnsConflictAndWritesNothing()
    {
        using var doc = new TempDocument("# External");
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save(new SaveInput("# Mine", ExpectedBaseline: "# Old"));

        Assert.Contains("\"outcome\":\"conflict\"", json);
        Assert.Equal("# External", File.ReadAllText(doc.Path)); // untouched
    }

    [Fact]
    public void Save_UncertainWrite_ReturnsUncertainAndKeepsTheNewerExternalData()
    {
        // A newer external write races the commit so AtomicFile cannot establish a safe state: the
        // save must surface an explicit uncertain outcome (not an ordinary no-write failure) and
        // must never clobber the newer external data.
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save(
            new SaveInput("# Mine", ExpectedBaseline: "# Old"),
            afterReplace: () => File.WriteAllText(doc.Path, "# Newer external"));

        Assert.Contains("\"outcome\":\"uncertain\"", json);
        Assert.Equal("# Newer external", File.ReadAllText(doc.Path)); // the newer data stands
    }

    [Fact]
    public void SaveConfig_UncertainWrite_ReturnsUncertainAndDoesNotAdvanceState()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);

        var json = session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", valid, "require-valid"),
            afterReplace: () => File.WriteAllText(configPath, Speaker("External", "E")));

        Assert.Contains("\"outcome\":\"uncertain\"", json);
        Assert.DoesNotContain("\"name\":\"Bob\"", json); // the session state never advanced past disk
        Assert.Equal(Speaker("External", "E"), File.ReadAllText(configPath)); // newer data preserved
    }

    [Fact]
    public void Save_ConfirmedOverwrite_BypassesTheBaselineCheck()
    {
        using var doc = new TempDocument("# External");
        var session = new LiveSession(doc.Path, "edit");

        var json = session.Save(
            new SaveInput("# Mine", ExpectedBaseline: "# Old", Conflict: "overwrite"));

        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Equal("# Mine", File.ReadAllText(doc.Path));
    }

    [Fact]
    public void Save_DiskAlreadyEqualsTheRequest_ReturnsIdempotentSaved()
    {
        using var doc = new TempDocument("# Same");
        var session = new LiveSession(doc.Path, "edit");

        // The disk already equals the requested source (a lost response), so a retry with a
        // different expected baseline still succeeds without a conflict.
        var json = session.Save(new SaveInput("# Same", ExpectedBaseline: "# Stale"));

        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Equal("# Same", File.ReadAllText(doc.Path));
    }

    [Fact]
    public async Task Save_ConcurrentSavesFromTheSameBaseline_ExactlyOneWinsTheOtherConflicts()
    {
        using var doc = new TempDocument("# Base");
        var session = new LiveSession(doc.Path, "edit");
        var ready = new Barrier(2);

        string SaveFrom(string source)
        {
            ready.SignalAndWait();
            return session.Save(new SaveInput(source, ExpectedBaseline: "# Base"));
        }

        var first = Task.Run(() => SaveFrom("# First"));
        var second = Task.Run(() => SaveFrom("# Second"));
        var results = await Task.WhenAll(first, second);

        // The exclusive compare-and-write serializes the two writes: whichever commits first is
        // the baseline the other now compares against, so exactly one saves and the other sees a
        // conflict — the lost update the non-atomic read-then-write allowed can no longer happen.
        Assert.Single(results, json => json.Contains("\"outcome\":\"saved\""));
        Assert.Single(results, json => json.Contains("\"outcome\":\"conflict\""));

        var winner = results[0].Contains("\"outcome\":\"saved\"") ? "# First" : "# Second";
        Assert.Equal(winner, File.ReadAllText(doc.Path));
    }

    [Fact]
    public async Task SaveConfig_ConcurrentSavesFromTheSameBaseline_ExactlyOneWinsTheOtherConflicts()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var baseline = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", baseline);
        var session = ConfiguredSession(docPath, configPath);
        var ready = new Barrier(2);

        string SaveFrom(string source)
        {
            ready.SignalAndWait();
            return session.Save(new SaveInput(source, "config", baseline, "require-valid"));
        }

        var first = Task.Run(() => SaveFrom(Speaker("Bob", "B")));
        var second = Task.Run(() => SaveFrom(Speaker("Cara", "C")));
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, json => json.Contains("\"outcome\":\"saved\""));
        Assert.Single(results, json => json.Contains("\"outcome\":\"conflict\""));
    }

    [Fact]
    public void Refresh_ExternalChangeBackToSelfWrittenContent_StillBroadcasts()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# B", ExpectedBaseline: "# Old"));
        session.Refresh(); // the browser's own write is suppressed once, consuming the token
        Assert.False(reader.TryRead(out _));

        File.WriteAllText(doc.Path, "# A");
        session.Refresh();
        Assert.True(reader.TryRead(out var toA));
        Assert.Contains("# A", toA!.Data);

        // An external change back to the earlier self-written content is a real external edit now,
        // not a stale self-write: one-shot suppression means it still reloads.
        File.WriteAllText(doc.Path, "# B");
        session.Refresh();
        Assert.True(reader.TryRead(out var backToB));
        Assert.Contains("# B", backToB!.Data);
    }

    [Fact]
    public void Refresh_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# Saved", ExpectedBaseline: "# Old"));
        session.Refresh(); // the watcher firing for the browser's own write

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void Refresh_ExternalChangeAfterSave_StillBroadcasts()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# Saved", ExpectedBaseline: "# Old"));
        File.WriteAllText(doc.Path, "# External");
        session.Refresh();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload", received!.Event);
        Assert.Contains("# External", received.Data);
    }

    [Fact]
    public void SaveConfig_ValidRequireValid_WritesAndRecompiles()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);

        var json = session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", Speaker("Alice", "A"), "require-valid"));

        Assert.Contains("\"outcome\":\"saved\"", json);
        Assert.Contains("Bob", File.ReadAllText(configPath));
        Assert.Contains("\"name\":\"Bob\"", json);
    }

    [Fact]
    public void SaveConfig_InvalidRequireValid_ReturnsInvalidAutoAndWritesNothing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        var json = session.Save(new SaveInput(broken, "config", valid, "require-valid"));

        Assert.Contains("\"outcome\":\"invalid-auto\"", json);
        Assert.Equal(valid, File.ReadAllText(configPath)); // Auto never writes invalid TOML
    }

    [Fact]
    public void SaveConfig_InvalidAllowInvalid_WritesAndReturnsSavedInvalid()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        var json = session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));

        Assert.Contains("\"outcome\":\"saved-invalid\"", json);
        Assert.Equal(broken, File.ReadAllText(configPath)); // persisted, like a force-write
        Assert.Contains("bogus", json); // the payload carries the invalid source for the editor
    }

    [Fact]
    public void SaveConfig_BaselineMismatch_ReturnsConflictAndWritesNothing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var disk = Speaker("External", "E");
        var configPath = tree.File("dialogue.toml", disk);
        var session = ConfiguredSession(docPath, configPath);

        var json = session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", Speaker("Alice", "A"), "require-valid"));

        Assert.Contains("\"outcome\":\"conflict\"", json);
        Assert.Equal(disk, File.ReadAllText(configPath));
    }

    [Fact]
    public void SaveConfig_WriteFailure_LeavesTheSessionStateDiskConsistent()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // directory permission bits do not block file creation the same way on Windows
        }

        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);

        // Make the config directory read-only so staging the replacement temp file fails after the
        // candidate config has been parsed. The published visualizer/config state must not advance
        // to a candidate that never reached disk — a page reload would otherwise render a config the
        // file does not hold.
        var mode = File.GetUnixFileMode(tree.Root);
        File.SetUnixFileMode(tree.Root, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            Assert.ThrowsAny<Exception>(() =>
                session.Save(new SaveInput(Speaker("Bob", "B"), "config", valid, "require-valid")));
        }
        finally
        {
            File.SetUnixFileMode(tree.Root, mode);
        }

        var json = session.CurrentDocumentJson();
        Assert.Contains("\"name\":\"Alice\"", json); // still the last committed config, not the candidate
        Assert.DoesNotContain("\"name\":\"Bob\"", json);
        Assert.DoesNotContain("\"configStatus\"", json); // state stayed valid, consistent with disk
        Assert.Equal(valid, File.ReadAllText(configPath)); // disk is unchanged
    }

    [Fact]
    public void SaveConfig_WithoutAConfigFile_Throws()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path, VisualizationMode.Edit);

        Assert.Throws<InvalidOperationException>(
            () => session.Save(new SaveInput(Speaker("Bob", "B"), "config")));
    }

    [Fact]
    public void Reload_Document_ReturnsLoadedWithTheDiskContent()
    {
        using var doc = new TempDocument("# Old");
        var session = new LiveSession(doc.Path, "edit");
        File.WriteAllText(doc.Path, "# External");

        var json = session.Reload(null);

        Assert.Contains("\"outcome\":\"loaded\"", json);
        Assert.Contains("# External", json);
    }

    [Fact]
    public void Reload_DeletedDocument_ReturnsMissing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var session = new LiveSession(docPath, "edit");
        File.Delete(docPath);

        var json = session.Reload(null);

        Assert.Contains("\"outcome\":\"missing\"", json);
    }

    [Fact]
    public void Reload_InvalidConfig_KeepsLastValidAndCarriesTheDiskSource()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        File.WriteAllText(configPath, "[[speakers]]\nbogus = true\n");

        var json = session.Reload("config");

        Assert.Contains("\"outcome\":\"invalid\"", json);
        Assert.Contains("bogus", json); // the external invalid TOML is carried for the editor
        Assert.Contains("\"name\":\"Alice\"", json); // last valid report is retained
    }

    [Fact]
    public void CreateConfig_WritesTheStarterFileAndAdoptsIt()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit); // no configuration
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        var result = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Created, result.Status);
        Assert.True(File.Exists(configPath));
        Assert.Contains("[[speakers]]", File.ReadAllText(configPath));
        Assert.Contains("\"outcome\":\"saved\"", result.Payload);
        Assert.Equal(configPath, session.ConfigPath); // adopted: the session now applies it
        // Staged temp + atomic move: the created file is complete and no staging temp is left behind.
        Assert.Equal(
            new[] { "dialogue.toml", "scene.dialogue.md" },
            Directory.GetFiles(tree.Root).Select(Path.GetFileName).OrderBy(name => name).ToArray());
        // Adopted: a later baseline-checked save recompiles with the new speaker.
        var saved = session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", ConfigStarter.Template, "require-valid"));
        Assert.Contains("\"name\":\"Bob\"", saved);
        Assert.Contains("Bob", File.ReadAllText(configPath));
    }

    [Fact]
    public void CreateConfig_ExistingTemplateOnDisk_AdoptsWithoutRewriting()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var configPath = tree.File("dialogue.toml", ConfigStarter.Template);
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Adopted, result.Status);
        Assert.Contains("\"outcome\":\"saved\"", result.Payload);
        Assert.Contains("dialogue.toml", result.Payload); // the payload now carries the configuration file
        Assert.Equal(configPath, session.ConfigPath);
    }

    [Fact]
    public void CreateConfig_ExistingDifferentContent_AdoptsItWithoutOverwriting()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var configPath = tree.File("dialogue.toml", "# hand-written\n");
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        // A config-less session recovers into the existing file rather than a dead end: it is
        // adopted without overwriting, ConfigPath is set, and the payload lets the frontend open it.
        Assert.Equal(CreateConfigStatus.AdoptedExisting, result.Status);
        Assert.Equal("# hand-written\n", File.ReadAllText(configPath)); // untouched
        Assert.Equal(configPath, session.ConfigPath); // no longer config-less
        Assert.Contains("\"outcome\":\"adopted\"", result.Payload);
    }

    [Fact]
    public void CreateConfig_ExistingInvalidContent_AdoptsAsSavedInvalid()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var invalid = "[[speakers]]\nbogus = true\n";
        var configPath = tree.File("dialogue.toml", invalid);
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.AdoptedExisting, result.Status);
        Assert.Equal(invalid, File.ReadAllText(configPath)); // untouched
        Assert.Equal(configPath, session.ConfigPath);
        Assert.Contains("\"outcome\":\"adopted-invalid\"", result.Payload);
        Assert.Contains("bogus", result.Payload); // the invalid source for the editor

        // The saved-invalid state persists so a page reload restores it (the overlay marks it).
        Assert.Contains("\"configStatus\":\"saved-invalid\"", session.CurrentDocumentJson());
    }

    [Fact]
    public void CreateConfig_ExistingInvalidContent_ReportCarriesConfigurationFileForRecovery()
    {
        // Adopting an invalid pre-existing dialogue.toml from a config-less session must still let
        // the frontend build a Config controller: the report needs a configuration.file with the
        // path and the (invalid) source, plus a saved-invalid status and message, both in the
        // adoption response and in the re-served page so a reload can edit and recover it.
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var invalid = "[[speakers]]\nbogus = true\n";
        var configPath = tree.File("dialogue.toml", invalid);
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        using var payload = JsonDocument.Parse(result.Payload);
        var file = payload.RootElement.GetProperty("configuration").GetProperty("file");
        Assert.Equal(configPath, file.GetProperty("path").GetString());
        Assert.Equal(invalid, file.GetProperty("source").GetString());
        Assert.Equal("saved-invalid", payload.RootElement.GetProperty("configStatus").GetString());
        Assert.False(
            string.IsNullOrEmpty(payload.RootElement.GetProperty("configMessage").GetString()));

        using var served = JsonDocument.Parse(session.CurrentDocumentJson());
        var servedFile = served.RootElement.GetProperty("configuration").GetProperty("file");
        Assert.Equal(configPath, servedFile.GetProperty("path").GetString());
        Assert.Equal(invalid, servedFile.GetProperty("source").GetString());
        Assert.Equal("saved-invalid", served.RootElement.GetProperty("configStatus").GetString());
    }

    [Fact]
    public void CreateConfig_WriteFails_LeavesNoConfigStateAndRetrySucceeds()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var missingDirectory = Path.Combine(tree.Root, "nope", "dialogue.toml");

        // The containing folder does not exist, so the exclusive create throws before writing.
        Assert.Throws<DirectoryNotFoundException>(() => session.CreateConfig(missingDirectory));
        Assert.Null(session.ConfigPath); // the no-config state is unchanged

        // A retry at a valid path still succeeds: _configPath never mutated on the failed attempt.
        var result = session.CreateConfig(Path.Combine(tree.Root, "dialogue.toml"));
        Assert.Equal(CreateConfigStatus.Created, result.Status);
    }

    [Fact]
    public void CurrentDocumentJson_AfterSavedInvalidConfig_CarriesTheInvalidSourceAndStatus()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));
        var json = session.CurrentDocumentJson();

        // A page reload re-serializes the current document: it must restore the saved-invalid state
        // (the invalid source and a stale report), not silently revert to the last valid text.
        Assert.Contains("\"configStatus\":\"saved-invalid\"", json);
        Assert.Contains("bogus", json); // the persisted invalid TOML
        Assert.Contains("\"name\":\"Alice\"", json); // the last valid speakers remain (stale)
    }

    [Fact]
    public void RenderInitialHtml_AfterSavedInvalidConfig_CarriesTheInvalidSourceAndStatus()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));
        var html = session.RenderInitialHtml();

        Assert.Contains("\"configStatus\":\"saved-invalid\"", html);
        Assert.Contains("bogus", html);
    }

    [Fact]
    public void CurrentDocumentJson_AfterConfigBecomesValidAgain_DropsTheStaleOverlay()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));
        session.Save(new SaveInput(Speaker("Bob", "B"), "config", broken, "require-valid"));
        var json = session.CurrentDocumentJson();

        Assert.DoesNotContain("\"configStatus\"", json);
        Assert.Contains("\"name\":\"Bob\"", json);
    }

    [Fact]
    public void CreateConfig_RetryAfterAdopt_IsIdempotentWhileTheFileIsStillTheTemplate()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        var created = session.CreateConfig(configPath);
        Assert.Equal(CreateConfigStatus.Created, created.Status);

        // The first response was lost, so the client retries the same create. The session already
        // adopted the file, but it is still the untouched template, so the retry is idempotent.
        var retry = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Adopted, retry.Status);
        Assert.Contains("\"outcome\":\"saved\"", retry.Payload);
        Assert.Equal(configPath, session.ConfigPath);
    }

    [Fact]
    public void CreateConfig_RetryAfterAdopt_DifferingContent_ReturnsConflict()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        session.CreateConfig(configPath);
        File.WriteAllText(configPath, Speaker("Alice", "A")); // the config diverged from the template

        var retry = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Conflict, retry.Status);
        Assert.Equal(Speaker("Alice", "A"), File.ReadAllText(configPath)); // untouched
    }

    [Fact]
    public void CreateConfig_WhenTheSessionAlreadyHasAConfig_Throws()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);

        Assert.Throws<InvalidOperationException>(
            () => session.CreateConfig(Path.Combine(tree.Root, "other.toml")));
    }

    [Fact]
    public void RefreshConfig_ExternalChange_BroadcastsAReloadConfigWithTheDiskContent()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.WriteAllText(configPath, Speaker("External", "E"));

        session.RefreshConfig();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("reload-config", received!.Event);
        Assert.Contains("External", received.Data);
    }

    [Fact]
    public void RefreshConfig_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput(Speaker("Bob", "B"), "config", valid, "require-valid"));
        session.RefreshConfig(); // the watcher firing for the browser's own config write

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void RefreshConfig_ExternalChangeBackToSelfWrittenContent_StillBroadcasts()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        var bob = Speaker("Bob", "B");
        session.Save(new SaveInput(bob, "config", valid, "require-valid"));
        session.RefreshConfig(); // the browser's own config write is suppressed once
        Assert.False(reader.TryRead(out _));

        File.WriteAllText(configPath, Speaker("External", "E"));
        session.RefreshConfig();
        Assert.True(reader.TryRead(out var toExternal));
        Assert.Contains("External", toExternal!.Data);

        File.WriteAllText(configPath, bob);
        session.RefreshConfig();
        Assert.True(reader.TryRead(out var backToBob));
        Assert.Contains("Bob", backToBob!.Data);
    }

    [Fact]
    public void RefreshConfig_DeletedConfiguration_BroadcastsAProblemTargetingConfig()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.Delete(configPath);

        session.RefreshConfig();

        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("not found", received.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"target\":\"config\"", received.Data); // routes through the config controller
    }

    [Fact]
    public void RefreshConfig_UnreadableConfiguration_BroadcastsAProblemInsteadOfThrowing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.Delete(configPath);
        Directory.CreateDirectory(configPath); // reading a directory throws UnauthorizedAccessException

        var problem = Record.Exception(() => session.RefreshConfig());

        Assert.Null(problem); // the timer callback never escapes
        Assert.True(reader.TryRead(out var received));
        Assert.Equal("problem", received!.Event);
        Assert.Contains("\"target\":\"config\"", received.Data);
    }

    [Fact]
    public void RefreshConfig_WithoutAConfigFile_DoesNothing()
    {
        using var doc = new TempDocument("# Scene");
        var session = new LiveSession(doc.Path, VisualizationMode.Edit);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.RefreshConfig();

        Assert.False(reader.TryRead(out _));
    }

    private static LiveSession ConfiguredSession(string docPath, string configPath)
    {
        var source = File.ReadAllText(configPath);
        var configuration = AppliedConfiguration.FromFile(
            configPath, source, TomlConfigurationLoader.Parse(source, configPath));
        return new LiveSession(
            docPath, VisualizationMode.Edit, new CompilationVisualizer(configuration), configPath);
    }

    private static string Speaker(string name, string id) =>
        $"[[speakers]]\nname = \"{name}\"\nid = \"{id}\"\n";
}
