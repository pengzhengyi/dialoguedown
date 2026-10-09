using DialogueDown.ConfigurationLoader;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Configuration;
using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Live.Serving;
using DialogueDown.Visualization.Live.Tests.Support;
using DialogueDown.Visualization.Render;
using static DialogueDown.Visualization.Live.Tests.Support.LivePageAssert;
using static DialogueDown.Visualization.Live.Tests.Support.LivePayload;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LiveSessionTests
{
    [Fact]
    public void RenderInitialHtml_EmbedsTheCurrentDocument()
    {
        using var script = new TempScript("# Scene");
        var session = new LiveSession(script.Path);

        var html = session.RenderInitialHtml();

        Assert.StartsWith("<!doctype html", html, StringComparison.OrdinalIgnoreCase);
        AssertPageEmbeds(html, session.CurrentDocumentJson());
    }

    [Fact]
    public void Mode_Default_IsView()
    {
        using var script = new TempScript("# Scene");

        var session = new LiveSession(script.Path);

        Assert.Equal("view", session.Mode);
        Assert.Equal("view", Parse(session.CurrentDocumentJson()).Mode);
    }

    [Fact]
    public void Mode_Explicit_IsCarriedIntoThePayload()
    {
        using var script = new TempScript("# Scene");

        var session = new LiveSession(script.Path, "edit");

        Assert.Equal("edit", session.Mode);
        Assert.Equal("edit", Parse(session.CurrentDocumentJson()).Mode);
    }

    [Fact]
    public void CurrentDocumentJson_CarriesPathSourceAndStages()
    {
        using var script = new TempScript("# Scene");
        var session = new LiveSession(script.Path);

        var document = Parse(session.CurrentDocumentJson());

        Assert.NotNull(document.Path);
        Assert.Equal("# Scene", document.Source);
        Assert.NotNull(document.Stages);
    }

    [Fact]
    public void Refresh_BroadcastsAReloadWithTheCurrentContent()
    {
        using var script = new TempScript("# First");
        var session = new LiveSession(script.Path);
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.WriteAllText(script.Path, "# Second");

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
        // The watcher calls Refresh from a timer callback, so an unreadable file, which throws
        // UnauthorizedAccessException, must become a problem event instead of escaping.
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
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");

        var saved = Parse(session.Save(new SaveInput("# New\n\nAlice: Hi", ExpectedBaseline: "# Old")));

        Assert.Equal("# New\n\nAlice: Hi", File.ReadAllText(script.Path));
        Assert.Equal("saved", saved.Outcome);
        Assert.Equal("# New\n\nAlice: Hi", saved.Source);
        Assert.NotNull(saved.Stages);
    }

    [Fact]
    public void RenderInitialHtml_ThroughASymlink_EmbedsTheDocumentAtTheLaunchedPath()
    {
        using var tree = new TempTree();
        var real = tree.File("real.dialogue.md", "# Scene");
        var link = Path.Combine(tree.Root, "link.dialogue.md");
        Symlinks.Create(link, real);
        var resolved = SymlinkResolver.Resolve(link);

        var session = new LiveSession(resolved, displayPath: link);
        var html = session.RenderInitialHtml();

        var document = session.CurrentDocumentJson();
        AssertPageEmbeds(html, document);
        Assert.Equal(link, Parse(document).Path); // the report shows the launched link path
        Assert.DoesNotContain("real.dialogue.md", document); // and never the resolved real target
        Assert.DoesNotContain("real.dialogue.md", html); // anywhere on the page
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

        var saved = Parse(json);
        Assert.Equal("saved", saved.Outcome);
        Assert.Equal("# New\n\nAlice: Hi", File.ReadAllText(real)); // the real target was written
        Assert.NotNull(new FileInfo(link).LinkTarget); // the link entry is preserved
        Assert.Equal(link, saved.Path); // the payload shows the launched link path
        Assert.DoesNotContain("real.dialogue.md", json); // and never the resolved real target
    }

    [Fact]
    public void Save_BaselineMismatch_ReturnsConflictAndWritesNothing()
    {
        using var script = new TempScript("# External");
        var session = new LiveSession(script.Path, "edit");

        var result = Parse(session.Save(new SaveInput("# Mine", ExpectedBaseline: "# Old")));

        Assert.Equal("conflict", result.Outcome);
        Assert.Equal("# External", File.ReadAllText(script.Path)); // untouched
    }

    [Fact]
    public void Save_UncertainWrite_ReturnsUncertainAndKeepsTheNewerExternalData()
    {
        // A newer external write lands during the commit, so the save cannot tell whether its
        // bytes are the ones on disk.
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");

        var result = Parse(session.Save(
            new SaveInput("# Mine", ExpectedBaseline: "# Old"),
            afterReplace: () => File.WriteAllText(script.Path, "# Newer external")));

        Assert.Equal("uncertain", result.Outcome);
        Assert.Equal("# Newer external", File.ReadAllText(script.Path)); // the newer data stands
    }

    [Fact]
    public void SaveConfig_UncertainWrite_ReturnsUncertainAndDoesNotAdvanceState()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);

        var result = Parse(session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", valid, "require-valid"),
            afterReplace: () => File.WriteAllText(configPath, Speaker("External", "E"))));

        Assert.Equal("uncertain", result.Outcome);
        Assert.DoesNotContain("Bob", result.SpeakerNames); // the session state never advanced past disk
        Assert.Equal(Speaker("External", "E"), File.ReadAllText(configPath)); // newer data preserved
    }

    [Fact]
    public void Save_ConfirmedOverwrite_BypassesTheBaselineCheck()
    {
        using var script = new TempScript("# External");
        var session = new LiveSession(script.Path, "edit");

        var result = Parse(session.Save(
            new SaveInput("# Mine", ExpectedBaseline: "# Old", Conflict: "overwrite")));

        Assert.Equal("saved", result.Outcome);
        Assert.Equal("# Mine", File.ReadAllText(script.Path));
    }

    [Fact]
    public void Save_DiskAlreadyEqualsTheRequest_ReturnsIdempotentSaved()
    {
        using var script = new TempScript("# Same");
        var session = new LiveSession(script.Path, "edit");

        // The disk already equals the requested source (a lost response), so a retry with a
        // different expected baseline still succeeds without a conflict.
        var result = Parse(session.Save(new SaveInput("# Same", ExpectedBaseline: "# Stale")));

        Assert.Equal("saved", result.Outcome);
        Assert.Equal("# Same", File.ReadAllText(script.Path));
    }

    [Fact]
    public async Task Save_ConcurrentSavesFromTheSameBaseline_ExactlyOneWinsTheOtherConflicts()
    {
        using var script = new TempScript("# Base");
        var session = new LiveSession(script.Path, "edit");
        var ready = new Barrier(2);

        string? SaveFrom(string source)
        {
            ready.SignalAndWait();
            return Parse(session.Save(new SaveInput(source, ExpectedBaseline: "# Base"))).Outcome;
        }

        var first = Task.Run(() => SaveFrom("# First"));
        var second = Task.Run(() => SaveFrom("# Second"));
        var outcomes = await Task.WhenAll(first, second);

        // The compare-and-write is exclusive: whichever save commits first changes the baseline
        // the other compares against.
        Assert.Equal(["conflict", "saved"], outcomes.Order());

        var winner = outcomes[0] == "saved" ? "# First" : "# Second";
        Assert.Equal(winner, File.ReadAllText(script.Path));
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

        string? SaveFrom(string source)
        {
            ready.SignalAndWait();
            return Parse(session.Save(new SaveInput(source, "config", baseline, "require-valid"))).Outcome;
        }

        var first = Task.Run(() => SaveFrom(Speaker("Bob", "B")));
        var second = Task.Run(() => SaveFrom(Speaker("Cara", "C")));
        var outcomes = await Task.WhenAll(first, second);

        Assert.Equal(["conflict", "saved"], outcomes.Order());
    }

    [Fact]
    public void Refresh_ExternalChangeBackToSelfWrittenContent_StillBroadcasts()
    {
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# B", ExpectedBaseline: "# Old"));
        session.Refresh(); // the browser's own write is suppressed once, consuming the token
        Assert.False(reader.TryRead(out _));

        File.WriteAllText(script.Path, "# A");
        session.Refresh();
        Assert.True(reader.TryRead(out var toA));
        Assert.Contains("# A", toA!.Data);

        // Writing the earlier self-written content back is an external edit, because the
        // suppression applies only once.
        File.WriteAllText(script.Path, "# B");
        session.Refresh();
        Assert.True(reader.TryRead(out var backToB));
        Assert.Contains("# B", backToB!.Data);
    }

    [Fact]
    public void Refresh_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# Saved", ExpectedBaseline: "# Old"));
        session.Refresh(); // the watcher firing for the browser's own write

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void Refresh_ExternalChangeAfterSave_StillBroadcasts()
    {
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# Saved", ExpectedBaseline: "# Old"));
        File.WriteAllText(script.Path, "# External");
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

        var saved = Parse(session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", Speaker("Alice", "A"), "require-valid")));

        Assert.Equal("saved", saved.Outcome);
        Assert.Contains("Bob", File.ReadAllText(configPath));
        Assert.Equal(["Bob"], saved.SpeakerNames);
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

        var result = Parse(session.Save(new SaveInput(broken, "config", valid, "require-valid")));

        Assert.Equal("invalid-auto", result.Outcome);
        Assert.Equal(valid, File.ReadAllText(configPath)); // require-valid never writes invalid TOML
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

        var saved = Parse(session.Save(new SaveInput(broken, "config", valid, "allow-invalid")));

        Assert.Equal("saved-invalid", saved.Outcome);
        Assert.Equal(broken, File.ReadAllText(configPath)); // persisted, like a force-write
        Assert.Equal(broken, saved.ConfigSource); // the payload carries the invalid source for the editor
    }

    [Fact]
    public void SaveConfig_BaselineMismatch_ReturnsConflictAndWritesNothing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var disk = Speaker("External", "E");
        var configPath = tree.File("dialogue.toml", disk);
        var session = ConfiguredSession(docPath, configPath);

        var result = Parse(session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", Speaker("Alice", "A"), "require-valid")));

        Assert.Equal("conflict", result.Outcome);
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

        // A read-only config directory makes staging the replacement fail after the candidate
        // config has been parsed. The session must keep the config the file holds, so a page
        // reload shows what is on disk.
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

        var document = Parse(session.CurrentDocumentJson());
        Assert.Equal(["Alice"], document.SpeakerNames); // still the last committed config, not the candidate
        Assert.False(document.Json.ContainsKey("configStatus")); // state stayed valid, consistent with disk
        Assert.Equal(valid, File.ReadAllText(configPath)); // disk is unchanged
    }

    [Fact]
    public void SaveConfig_WithoutAConfigFile_Throws()
    {
        using var script = new TempScript("# Scene");
        var session = new LiveSession(script.Path, VisualizationMode.Edit);

        Assert.Throws<InvalidOperationException>(
            () => session.Save(new SaveInput(Speaker("Bob", "B"), "config")));
    }

    [Fact]
    public void Reload_Document_ReturnsLoadedWithTheDiskContent()
    {
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");
        File.WriteAllText(script.Path, "# External");

        var loaded = Parse(session.Reload(null));

        Assert.Equal("loaded", loaded.Outcome);
        Assert.Equal("# External", loaded.Source);
    }

    [Fact]
    public void Reload_DeletedDocument_ReturnsMissing()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var session = new LiveSession(docPath, "edit");
        File.Delete(docPath);

        var result = Parse(session.Reload(null));

        Assert.Equal("missing", result.Outcome);
    }

    [Fact]
    public void Reload_InvalidConfig_KeepsLastValidAndCarriesTheDiskSource()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var configPath = tree.File("dialogue.toml", Speaker("Alice", "A"));
        var session = ConfiguredSession(docPath, configPath);
        File.WriteAllText(configPath, "[[speakers]]\nbogus = true\n");

        var result = Parse(session.Reload("config"));

        Assert.Equal("invalid", result.Outcome);
        Assert.Equal("[[speakers]]\nbogus = true\n", result.ConfigSource); // the external invalid TOML, for the editor
        Assert.Equal(["Alice"], result.SpeakerNames); // last valid report is retained
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
        Assert.Equal("saved", Parse(result.Payload).Outcome);
        Assert.Equal(configPath, session.ConfigPath); // adopted: the session now applies it
        // No staging temp file is left behind.
        Assert.Equal(
            new[] { "dialogue.toml", "scene.dialogue.md" },
            Directory.GetFiles(tree.Root).Select(Path.GetFileName).OrderBy(name => name).ToArray());
        // Adopted: a later baseline-checked save recompiles with the new speaker.
        var saved = Parse(session.Save(
            new SaveInput(Speaker("Bob", "B"), "config", ConfigStarter.Template, "require-valid")));
        Assert.Equal(["Bob"], saved.SpeakerNames);
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

        var adopted = Parse(result.Payload);
        Assert.Equal(CreateConfigStatus.Adopted, result.Status);
        Assert.Equal("saved", adopted.Outcome);
        Assert.Equal(configPath, adopted.ConfigPath); // the payload now carries the configuration file
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

        Assert.Equal(CreateConfigStatus.AdoptedExisting, result.Status);
        Assert.Equal("# hand-written\n", File.ReadAllText(configPath)); // untouched
        Assert.Equal(configPath, session.ConfigPath); // no longer config-less
        Assert.Equal("adopted", Parse(result.Payload).Outcome);
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

        var adopted = Parse(result.Payload);
        Assert.Equal(CreateConfigStatus.AdoptedExisting, result.Status);
        Assert.Equal(invalid, File.ReadAllText(configPath)); // untouched
        Assert.Equal(configPath, session.ConfigPath);
        Assert.Equal("adopted-invalid", adopted.Outcome);
        Assert.Equal(invalid, adopted.ConfigSource); // the invalid source for the editor

        // The saved-invalid state is kept, so a page reload restores it.
        Assert.Equal("saved-invalid", Parse(session.CurrentDocumentJson()).ConfigStatus);
    }

    [Fact]
    public void CreateConfig_ExistingInvalidContent_ReportCarriesConfigurationFileForRecovery()
    {
        // The report client builds its config editor from configuration.file, so after adopting
        // an invalid file both the response and the re-served page carry its path and source,
        // with a saved-invalid status and message.
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene");
        var invalid = "[[speakers]]\nbogus = true\n";
        var configPath = tree.File("dialogue.toml", invalid);
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        var adopted = Parse(result.Payload);
        Assert.Equal(configPath, adopted.ConfigPath);
        Assert.Equal(invalid, adopted.ConfigSource);
        Assert.Equal("saved-invalid", adopted.ConfigStatus);
        Assert.False(string.IsNullOrEmpty(adopted.ConfigMessage));

        var served = Parse(session.CurrentDocumentJson());
        Assert.Equal(configPath, served.ConfigPath);
        Assert.Equal(invalid, served.ConfigSource);
        Assert.Equal("saved-invalid", served.ConfigStatus);
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
        var document = Parse(session.CurrentDocumentJson());

        // A page reload re-serializes the current document: it must restore the saved-invalid state
        // (the invalid source and a stale report), not silently revert to the last valid text.
        Assert.Equal("saved-invalid", document.ConfigStatus);
        Assert.Equal(broken, document.ConfigSource); // the persisted invalid TOML
        Assert.Equal(["Alice"], document.SpeakerNames); // the last valid speakers remain (stale)
    }

    [Fact]
    public void RenderInitialHtml_AfterSavedInvalidConfig_EmbedsTheSavedInvalidDocument()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var valid = Speaker("Alice", "A");
        var configPath = tree.File("dialogue.toml", valid);
        var session = ConfiguredSession(docPath, configPath);
        var broken = "[[speakers]]\nbogus = true\n";

        session.Save(new SaveInput(broken, "config", valid, "allow-invalid"));
        var html = session.RenderInitialHtml();

        // CurrentDocumentJson_AfterSavedInvalidConfig_CarriesTheInvalidSourceAndStatus asserts what
        // that document carries; this asserts the page embeds it, once the save reached saved-invalid.
        AssertPageEmbeds(html, session.CurrentDocumentJson());
        Assert.Equal("saved-invalid", FromPage(html).ConfigStatus);
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
        var document = Parse(session.CurrentDocumentJson());

        Assert.False(document.Json.ContainsKey("configStatus"));
        Assert.Equal(["Bob"], document.SpeakerNames);
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
        Assert.Equal("saved", Parse(retry.Payload).Outcome);
        Assert.Equal(configPath, session.ConfigPath);
    }

    [Fact]
    public void CreateConfig_RetryWithAnotherSpellingOfItsPath_MatchesByTheMachinesPathRule()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", "# Scene\n\nAlice: Hi.");
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var configPath = Path.Combine(tree.Root, "dialogue.toml");
        session.CreateConfig(configPath);
        var respelled = Path.Combine(tree.Root, "Dialogue.toml");

        // Where file names ignore case, both spellings name the session's own config, so the
        // retry is answered like any other; where they do not, it names a second file.
        if (PathComparison.Comparer.Equals(configPath, respelled))
        {
            Assert.Equal(CreateConfigStatus.Adopted, session.CreateConfig(respelled).Status);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => session.CreateConfig(respelled));
        }
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
        using var script = new TempScript("# Scene");
        var session = new LiveSession(script.Path, VisualizationMode.Edit);
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
