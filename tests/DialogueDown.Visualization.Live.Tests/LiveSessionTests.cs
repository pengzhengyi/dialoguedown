using DialogueDown.ConfigurationLoader;
using DialogueDown.TestSupport;
using DialogueDown.Visualization.Configuration;
using DialogueDown.Visualization.Live.Configuration;
using DialogueDown.Visualization.Live.Files;
using DialogueDown.Visualization.Live.Serving;
using DialogueDown.Visualization.Live.Tests.Support;
using DialogueDown.Visualization.Render;
using static DialogueDown.Visualization.Live.Tests.Support.LiveEventAssert;
using static DialogueDown.Visualization.Live.Tests.Support.LivePageAssert;
using static DialogueDown.Visualization.Live.Tests.Support.LivePayload;

namespace DialogueDown.Visualization.Live.Tests;

public sealed class LiveSessionTests
{
    private const string AScene = """
        # Scene

        Alice: Hi.
        """;

    private const string AnEditedScene = """
        # New

        Alice: Hi
        """;

    /// <summary>A <c>dialogue.toml</c> that does not parse: a speaker with a key the schema lacks.</summary>
    private const string AnInvalidConfig = """
        [[speakers]]
        bogus = true

        """;

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

        Assert.Equal("# Second", AssertBroadcast(reader, "reload").Source);
    }

    [Fact]
    public void Refresh_MissingDocument_BroadcastsAProblem()
    {
        var session = new LiveSession(Path.Combine(Path.GetTempPath(), "missing-edit.dialogue.md"));
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Refresh();

        var problem = AssertBroadcast(reader, "problem");
        Assert.Contains("not found", problem.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("document", problem.Target); // routes through the document controller
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
        Assert.Equal("document", AssertBroadcast(reader, "problem").Target);
    }

    [Fact]
    public void Save_MatchingBaseline_WritesTheBufferAndReturnsSaved()
    {
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");

        var saved = Parse(session.Save(new SaveInput(AnEditedScene, ExpectedBaseline: "# Old")));

        Assert.Equal(AnEditedScene, File.ReadAllText(script.Path));
        Assert.Equal("saved", saved.Outcome);
        Assert.Equal(AnEditedScene, saved.Source);
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
        var json = session.Save(new SaveInput(AnEditedScene, ExpectedBaseline: "# Old"));

        var saved = Parse(json);
        Assert.Equal("saved", saved.Outcome);
        Assert.Equal(AnEditedScene, File.ReadAllText(real)); // the real target was written
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
        var valid = ASpeakerConfig("Alice", "A");
        var (session, configPath) = ASessionWithConfig(tree, valid);

        var result = Parse(session.Save(
            ConfigSave(ASpeakerConfig("Bob", "B"), valid),
            afterReplace: () => File.WriteAllText(configPath, ASpeakerConfig("External", "E"))));

        Assert.Equal("uncertain", result.Outcome);
        // An uncertain result carries only its outcome and message, so the session's state is read
        // from its current document: it still applies the config it last committed.
        Assert.Equal(["Alice"], Parse(session.CurrentDocumentJson()).SpeakerNames);
        Assert.Equal(ASpeakerConfig("External", "E"), File.ReadAllText(configPath)); // newer data preserved
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
        var baseline = ASpeakerConfig("Alice", "A");
        var (session, _) = ASessionWithConfig(tree, baseline);
        var ready = new Barrier(2);

        string? SaveFrom(string source)
        {
            ready.SignalAndWait();
            return Parse(session.Save(ConfigSave(source, baseline))).Outcome;
        }

        var first = Task.Run(() => SaveFrom(ASpeakerConfig("Bob", "B")));
        var second = Task.Run(() => SaveFrom(ASpeakerConfig("Cara", "C")));
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
        AssertNothingBroadcast(reader);

        File.WriteAllText(script.Path, "# A");
        session.Refresh();
        Assert.Equal("# A", AssertBroadcast(reader, "reload").Source);

        // Writing the earlier self-written content back is an external edit, because the
        // suppression applies only once.
        File.WriteAllText(script.Path, "# B");
        session.Refresh();
        Assert.Equal("# B", AssertBroadcast(reader, "reload").Source);
    }

    [Fact]
    public void Refresh_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var script = new TempScript("# Old");
        var session = new LiveSession(script.Path, "edit");
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(new SaveInput("# Saved", ExpectedBaseline: "# Old"));
        session.Refresh(); // the watcher firing for the browser's own write

        AssertNothingBroadcast(reader);
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

        Assert.Equal("# External", AssertBroadcast(reader, "reload").Source);
    }

    [Fact]
    public void SaveConfig_ValidRequireValid_WritesAndRecompiles()
    {
        using var tree = new TempTree();
        var (session, configPath) = ASessionWithConfig(tree, ASpeakerConfig("Alice", "A"));

        var saved = Parse(session.Save(
            ConfigSave(ASpeakerConfig("Bob", "B"), ASpeakerConfig("Alice", "A"))));

        Assert.Equal("saved", saved.Outcome);
        Assert.Contains("Bob", File.ReadAllText(configPath));
        Assert.Equal(["Bob"], saved.SpeakerNames);
    }

    [Fact]
    public void SaveConfig_InvalidRequireValid_ReturnsInvalidAutoAndWritesNothing()
    {
        using var tree = new TempTree();
        var valid = ASpeakerConfig("Alice", "A");
        var (session, configPath) = ASessionWithConfig(tree, valid);

        var result = Parse(session.Save(ConfigSave(AnInvalidConfig, valid)));

        Assert.Equal("invalid-auto", result.Outcome);
        Assert.Equal(valid, File.ReadAllText(configPath)); // require-valid never writes invalid TOML
    }

    [Fact]
    public void SaveConfig_InvalidAllowInvalid_WritesAndReturnsSavedInvalid()
    {
        using var tree = new TempTree();
        var valid = ASpeakerConfig("Alice", "A");
        var (session, configPath) = ASessionWithConfig(tree, valid);

        var saved = Parse(session.Save(ConfigSave(AnInvalidConfig, valid, "allow-invalid")));

        Assert.Equal("saved-invalid", saved.Outcome);
        Assert.Equal(AnInvalidConfig, File.ReadAllText(configPath)); // persisted, like a force-write
        Assert.Equal(AnInvalidConfig, saved.ConfigSource); // the payload carries the invalid source for the editor
    }

    [Fact]
    public void SaveConfig_BaselineMismatch_ReturnsConflictAndWritesNothing()
    {
        using var tree = new TempTree();
        var disk = ASpeakerConfig("External", "E");
        var (session, configPath) = ASessionWithConfig(tree, disk);

        var result = Parse(session.Save(
            ConfigSave(ASpeakerConfig("Bob", "B"), ASpeakerConfig("Alice", "A"))));

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
        var valid = ASpeakerConfig("Alice", "A");
        var (session, configPath) = ASessionWithConfig(tree, valid);

        // A read-only config directory makes staging the replacement fail after the candidate
        // config has been parsed. The session must keep the config the file holds, so a page
        // reload shows what is on disk.
        var mode = File.GetUnixFileMode(tree.Root);
        File.SetUnixFileMode(tree.Root, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            Assert.ThrowsAny<Exception>(() =>
                session.Save(ConfigSave(ASpeakerConfig("Bob", "B"), valid)));
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
            () => session.Save(new SaveInput(ASpeakerConfig("Bob", "B"), Target: "config")));
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
        var (session, configPath) = ASessionWithConfig(tree, ASpeakerConfig("Alice", "A"));
        File.WriteAllText(configPath, AnInvalidConfig);

        var result = Parse(session.Reload("config"));

        Assert.Equal("invalid", result.Outcome);
        Assert.Equal(AnInvalidConfig, result.ConfigSource); // the external invalid TOML, for the editor
        Assert.Equal(["Alice"], result.SpeakerNames); // last valid report is retained
    }

    [Fact]
    public void CreateConfig_WritesTheStarterFileAndAdoptsIt()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", AScene);
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
            ConfigSave(ASpeakerConfig("Bob", "B"), ConfigStarter.Template)));
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
        var configPath = tree.File("dialogue.toml", AnInvalidConfig);
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        var adopted = Parse(result.Payload);
        Assert.Equal(CreateConfigStatus.AdoptedExisting, result.Status);
        Assert.Equal(AnInvalidConfig, File.ReadAllText(configPath)); // untouched
        Assert.Equal(configPath, session.ConfigPath);
        Assert.Equal("adopted-invalid", adopted.Outcome);
        Assert.Equal(AnInvalidConfig, adopted.ConfigSource); // the invalid source for the editor

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
        var configPath = tree.File("dialogue.toml", AnInvalidConfig);
        var session = new LiveSession(docPath, VisualizationMode.Edit);

        var result = session.CreateConfig(configPath);

        var adopted = Parse(result.Payload);
        Assert.Equal(configPath, adopted.ConfigPath);
        Assert.Equal(AnInvalidConfig, adopted.ConfigSource);
        Assert.Equal("saved-invalid", adopted.ConfigStatus);
        Assert.False(string.IsNullOrEmpty(adopted.ConfigMessage));

        var served = Parse(session.CurrentDocumentJson());
        Assert.Equal(configPath, served.ConfigPath);
        Assert.Equal(AnInvalidConfig, served.ConfigSource);
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
        var valid = ASpeakerConfig("Alice", "A");
        var (session, _) = ASessionWithConfig(tree, valid);

        session.Save(ConfigSave(AnInvalidConfig, valid, "allow-invalid"));
        var document = Parse(session.CurrentDocumentJson());

        // A page reload re-serializes the current document: it must restore the saved-invalid state
        // (the invalid source and a stale report), not silently revert to the last valid text.
        Assert.Equal("saved-invalid", document.ConfigStatus);
        Assert.Equal(AnInvalidConfig, document.ConfigSource); // the persisted invalid TOML
        Assert.Equal(["Alice"], document.SpeakerNames); // the last valid speakers remain (stale)
    }

    [Fact]
    public void RenderInitialHtml_AfterSavedInvalidConfig_EmbedsTheSavedInvalidDocument()
    {
        using var tree = new TempTree();
        var valid = ASpeakerConfig("Alice", "A");
        var (session, _) = ASessionWithConfig(tree, valid);

        session.Save(ConfigSave(AnInvalidConfig, valid, "allow-invalid"));
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
        var valid = ASpeakerConfig("Alice", "A");
        var (session, _) = ASessionWithConfig(tree, valid);

        session.Save(ConfigSave(AnInvalidConfig, valid, "allow-invalid"));
        session.Save(ConfigSave(ASpeakerConfig("Bob", "B"), AnInvalidConfig));
        var document = Parse(session.CurrentDocumentJson());

        Assert.False(document.Json.ContainsKey("configStatus"));
        Assert.Equal(["Bob"], document.SpeakerNames);
    }

    [Fact]
    public void CreateConfig_RetryAfterAdopt_IsIdempotentWhileTheFileIsStillTheTemplate()
    {
        using var tree = new TempTree();
        var docPath = tree.File("scene.dialogue.md", AScene);
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
        var docPath = tree.File("scene.dialogue.md", AScene);
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
        var docPath = tree.File("scene.dialogue.md", AScene);
        var session = new LiveSession(docPath, VisualizationMode.Edit);
        var configPath = Path.Combine(tree.Root, "dialogue.toml");

        session.CreateConfig(configPath);
        File.WriteAllText(configPath, ASpeakerConfig("Alice", "A")); // the config diverged from the template

        var retry = session.CreateConfig(configPath);

        Assert.Equal(CreateConfigStatus.Conflict, retry.Status);
        Assert.Equal(ASpeakerConfig("Alice", "A"), File.ReadAllText(configPath)); // untouched
    }

    [Fact]
    public void CreateConfig_WhenTheSessionAlreadyHasAConfig_Throws()
    {
        using var tree = new TempTree();
        var (session, _) = ASessionWithConfig(tree, ASpeakerConfig("Alice", "A"));

        Assert.Throws<InvalidOperationException>(
            () => session.CreateConfig(Path.Combine(tree.Root, "other.toml")));
    }

    [Fact]
    public void RefreshConfig_ExternalChange_BroadcastsAReloadConfigWithTheDiskContent()
    {
        using var tree = new TempTree();
        var (session, configPath) = ASessionWithConfig(tree, ASpeakerConfig("Alice", "A"));
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.WriteAllText(configPath, ASpeakerConfig("External", "E"));

        session.RefreshConfig();

        Assert.Equal(ASpeakerConfig("External", "E"), AssertBroadcast(reader, "reload-config").ConfigSource);
    }

    [Fact]
    public void RefreshConfig_AfterSave_SuppressesTheSelfTriggeredReload()
    {
        using var tree = new TempTree();
        var valid = ASpeakerConfig("Alice", "A");
        var (session, _) = ASessionWithConfig(tree, valid);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.Save(ConfigSave(ASpeakerConfig("Bob", "B"), valid));
        session.RefreshConfig(); // the watcher firing for the browser's own config write

        AssertNothingBroadcast(reader);
    }

    [Fact]
    public void RefreshConfig_ExternalChangeBackToSelfWrittenContent_StillBroadcasts()
    {
        using var tree = new TempTree();
        var valid = ASpeakerConfig("Alice", "A");
        var (session, configPath) = ASessionWithConfig(tree, valid);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        var bob = ASpeakerConfig("Bob", "B");
        session.Save(ConfigSave(bob, valid));
        session.RefreshConfig(); // the browser's own config write is suppressed once
        AssertNothingBroadcast(reader);

        File.WriteAllText(configPath, ASpeakerConfig("External", "E"));
        session.RefreshConfig();
        Assert.Equal(ASpeakerConfig("External", "E"), AssertBroadcast(reader, "reload-config").ConfigSource);

        File.WriteAllText(configPath, bob);
        session.RefreshConfig();
        Assert.Equal(bob, AssertBroadcast(reader, "reload-config").ConfigSource);
    }

    [Fact]
    public void RefreshConfig_DeletedConfiguration_BroadcastsAProblemTargetingConfig()
    {
        using var tree = new TempTree();
        var (session, configPath) = ASessionWithConfig(tree, ASpeakerConfig("Alice", "A"));
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.Delete(configPath);

        session.RefreshConfig();

        var problem = AssertBroadcast(reader, "problem");
        Assert.Contains("not found", problem.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("config", problem.Target); // routes through the config controller
    }

    [Fact]
    public void RefreshConfig_UnreadableConfiguration_BroadcastsAProblemInsteadOfThrowing()
    {
        using var tree = new TempTree();
        var (session, configPath) = ASessionWithConfig(tree, ASpeakerConfig("Alice", "A"));
        using var subscription = session.Broadcaster.Subscribe(out var reader);
        File.Delete(configPath);
        Directory.CreateDirectory(configPath); // reading a directory throws UnauthorizedAccessException

        var problem = Record.Exception(() => session.RefreshConfig());

        Assert.Null(problem); // the timer callback never escapes
        Assert.Equal("config", AssertBroadcast(reader, "problem").Target);
    }

    [Fact]
    public void RefreshConfig_WithoutAConfigFile_DoesNothing()
    {
        using var script = new TempScript("# Scene");
        var session = new LiveSession(script.Path, VisualizationMode.Edit);
        using var subscription = session.Broadcaster.Subscribe(out var reader);

        session.RefreshConfig();

        AssertNothingBroadcast(reader);
    }

    /// <summary>A session editing <see cref="AScene"/>, whose <c>dialogue.toml</c> holds <paramref name="config"/>.</summary>
    /// <param name="tree">The folder the script and its config are written into.</param>
    /// <param name="config">What <c>dialogue.toml</c> says.</param>
    /// <returns>A session in edit mode that applies the config, and where the config was written.</returns>
    private static (LiveSession Session, string ConfigPath) ASessionWithConfig(TempTree tree, string config)
    {
        var docPath = tree.File("scene.dialogue.md", AScene);
        var configPath = tree.File("dialogue.toml", config);
        var configuration = AppliedConfiguration.FromFile(
            configPath, config, TomlConfigurationLoader.Parse(config, configPath));
        var session = new LiveSession(
            docPath, VisualizationMode.Edit, new CompilationVisualizer(configuration), configPath);

        return (session, configPath);
    }

    /// <summary>A request to save the config, checked against the text the page last loaded.</summary>
    private static SaveInput ConfigSave(string source, string baseline, string validation = "require-valid") =>
        new(source, Target: "config", ExpectedBaseline: baseline, Validation: validation);

    /// <summary>A <c>dialogue.toml</c> declaring one speaker.</summary>
    private static string ASpeakerConfig(string name, string id) => $"""
        [[speakers]]
        name = "{name}"
        id = "{id}"

        """;
}
