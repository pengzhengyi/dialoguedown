// @vitest-environment node

import { describe, it, expect, vi } from "vitest";
import { createModeController, type ModeControllerPorts } from "./view-edit";
import type { LiveEditController } from "./live-edit";

function fakeLive(dirty = false): LiveEditController {
    return {
        onEdit: vi.fn(),
        save: vi.fn(async () => "saved" as const),
        flush: vi.fn(async () => "saved" as const),
        onDiskChange: vi.fn(),
        reload: vi.fn(async () => "saved" as const),
        adoptDisk: vi.fn(),
        whenIdle: vi.fn(async () => {}),
        discard: vi.fn(),
        discardChanges: vi.fn(),
        setMode: vi.fn(),
        mode: "manual",
        dirty,
        status: dirty ? "dirty" : "saved",
        statusMessage: undefined,
        canDiscard: true,
    };
}

function fakePorts(overrides: Partial<ModeControllerPorts> = {}) {
    const app = {
        updateStages: vi.fn(),
        setEditable: vi.fn(),
        setContent: vi.fn(),
        setDiagnostics: vi.fn(),
        setSemanticTokens: vi.fn(),
        setReservedTargets: vi.fn(),
        setConfigEditable: vi.fn(),
        setConfigContent: vi.fn(),
        updateConfig: vi.fn(),
        setConfigStale: vi.fn(),
        markSourceDirty: vi.fn(),
        markConfigDirty: vi.fn(),
        isConfigTabActive: vi.fn(() => false),
        showConfigTab: vi.fn(),
        showBanner: vi.fn(),
    };
    const ports: ModeControllerPorts = {
        app,
        dialogueLive: fakeLive(),
        configLive: null,
        setEditControlsVisible: vi.fn(),
        reflect: vi.fn(),
        resolveDocument: vi.fn(async () => true),
        ...overrides,
    };
    return { ports, app };
}

/** Let the async leave-Edit flush settle. */
const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

describe("createModeController", () => {
    it("configures the editor for the initial mode", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        expect(c.mode).toBe("view");
        expect(app.setEditable).toHaveBeenCalledWith(false);
        expect(ports.setEditControlsVisible).toHaveBeenCalledWith(false);
        expect(ports.reflect).toHaveBeenCalledWith("view");
    });

    it("tracks edits only in Edit mode", () => {
        const { ports } = fakePorts();
        const c = createModeController("view", ports);

        c.onEditorChange("# a"); // View: a reload, not a user edit
        expect(ports.dialogueLive.onEdit).not.toHaveBeenCalled();

        c.switchTo("edit");
        c.onEditorChange("# b");
        expect(ports.dialogueLive.onEdit).toHaveBeenCalledWith("# b");
    });

    it("re-syncs the editor and graphs on a reload in View", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        const dialogueReport = {
            source: "# fresh",
            stages: [],
            diagnostics: [
                {
                    range: { start: { line: 0, character: 0 }, end: { line: 0, character: 1 } },
                    severity: 1,
                    code: "DLG2001",
                    message: "Boom.",
                    source: "dialoguedown",
                },
            ],
            semanticTokens: [
                {
                    range: { start: { line: 0, character: 0 }, end: { line: 0, character: 1 } },
                    kind: "ReservedTag",
                },
            ],
            symbols: {
                reservedTargets: [{ anchor: "END", label: "End", role: "Terminal" }],
            },
        } as unknown as Parameters<typeof c.onReload>[0];
        c.onReload(dialogueReport);

        expect(app.setContent).toHaveBeenCalledWith("# fresh");
        expect(app.updateStages).toHaveBeenCalledWith([]);
        expect(app.setDiagnostics).toHaveBeenCalledWith([
            {
                range: { start: { line: 0, character: 0 }, end: { line: 0, character: 1 } },
                severity: 1,
                code: "DLG2001",
                message: "Boom.",
                source: "dialoguedown",
            },
        ]);
        expect(app.setSemanticTokens).toHaveBeenCalledWith([
            {
                range: { start: { line: 0, character: 0 }, end: { line: 0, character: 1 } },
                kind: "ReservedTag",
            },
        ]);
        expect(app.setReservedTargets).toHaveBeenCalledWith([
            { anchor: "END", label: "End", role: "Terminal" },
        ]);
        expect(app.showBanner).toHaveBeenCalledWith(null);
        expect(ports.dialogueLive.onDiskChange).not.toHaveBeenCalled();
        // The dialogue controller adopts the external content as its clean baseline.
        expect(ports.dialogueLive.adoptDisk).toHaveBeenCalledWith(
            "# fresh",
            true,
            undefined,
            dialogueReport,
        );
    });

    it("adopts an external config change into the config controller in View", () => {
        const configLive = fakeLive();
        const { ports, app } = fakePorts({ configLive });
        const c = createModeController("view", ports);

        const configReport = {
            stages: [],
            configuration: { file: { path: "dialogue.toml", source: 'mode = "best-effort"' } },
            symbols: {
                reservedTargets: [{ anchor: "END", label: "End", role: "Terminal" }],
            },
            outcome: "loaded",
        } as unknown as Parameters<typeof c.onReloadConfig>[0];
        c.onReloadConfig(configReport);

        expect(app.updateConfig).toHaveBeenCalledOnce();
        expect(app.setConfigContent).toHaveBeenCalledWith('mode = "best-effort"');
        expect(app.updateStages).toHaveBeenCalledWith([]);
        expect(app.setReservedTargets).toHaveBeenCalledWith([
            { anchor: "END", label: "End", role: "Terminal" },
        ]);
        expect(configLive.adoptDisk).toHaveBeenCalledWith(
            'mode = "best-effort"',
            true,
            undefined,
            configReport,
        );
        expect(configLive.onDiskChange).not.toHaveBeenCalled();
    });

    it("raises Conflict on the config controller for an external config change in Edit", () => {
        const configLive = fakeLive();
        const { ports, app } = fakePorts({ configLive });
        const c = createModeController("edit", ports);

        c.onReloadConfig({
            stages: [],
            configuration: { file: { path: "dialogue.toml", source: "x = 1" } },
            outcome: "loaded",
        } as unknown as Parameters<typeof c.onReloadConfig>[0]);

        expect(configLive.onDiskChange).toHaveBeenCalledOnce();
        expect(app.setConfigContent).not.toHaveBeenCalled();
        expect(configLive.adoptDisk).not.toHaveBeenCalled();
    });

    it("adopts an invalid external config as saved-invalid in View", () => {
        const configLive = fakeLive();
        const { ports } = fakePorts({ configLive });
        const c = createModeController("view", ports);

        const configReport = {
            stages: [],
            configuration: { file: { path: "dialogue.toml", source: "bogus" } },
            outcome: "invalid",
            configMessage: "line 1: bad toml",
        } as unknown as Parameters<typeof c.onReloadConfig>[0];
        c.onReloadConfig(configReport);

        // The invalid external content is adopted as saved-invalid, carrying its parse detail so a
        // later Discard restores the exact message.
        expect(configLive.adoptDisk).toHaveBeenCalledWith(
            "bogus",
            false,
            "line 1: bad toml",
            configReport,
        );
    });

    it("enters Conflict (never reloads) on an external disk change in Edit", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("edit", ports);

        c.onReload({ source: "# fresh", stages: [] });

        expect(ports.dialogueLive.onDiskChange).toHaveBeenCalledOnce();
        expect(app.setContent).not.toHaveBeenCalled();
    });

    it("routes a document disk problem through the dialogue controller in Edit", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("edit", ports);

        c.onProblem("Document not found: scene.dialogue.md", "document");

        expect(ports.dialogueLive.onDiskChange).toHaveBeenCalledWith(
            "Document not found: scene.dialogue.md",
        );
        expect(app.showBanner).not.toHaveBeenCalled(); // not banner-only
    });

    it("routes a config disk problem through the config controller in Edit", () => {
        const configLive = fakeLive();
        const { ports, app } = fakePorts({ configLive });
        const c = createModeController("edit", ports);

        c.onProblem("Configuration not found: dialogue.toml", "config");

        expect(configLive.onDiskChange).toHaveBeenCalledWith(
            "Configuration not found: dialogue.toml",
        );
        expect(ports.dialogueLive.onDiskChange).not.toHaveBeenCalled();
        expect(app.showBanner).not.toHaveBeenCalled();
    });

    it("banners a disk problem in View without touching a controller", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        c.onProblem("Document not found: scene.dialogue.md", "document");

        expect(app.showBanner).toHaveBeenCalledWith("Document not found: scene.dialogue.md");
        expect(ports.dialogueLive.onDiskChange).not.toHaveBeenCalled();
    });

    it("banners a config problem in Edit when the session has no config controller", () => {
        const { ports, app } = fakePorts({ configLive: null });
        const c = createModeController("edit", ports);

        c.onProblem("Configuration not found: dialogue.toml", "config");

        expect(app.showBanner).toHaveBeenCalledWith("Configuration not found: dialogue.toml");
    });

    it("switches View → Edit, making the editor editable", () => {
        const { ports, app } = fakePorts();
        const c = createModeController("view", ports);

        c.switchTo("edit");

        expect(c.mode).toBe("edit");
        expect(app.setEditable).toHaveBeenLastCalledWith(true);
        expect(ports.setEditControlsVisible).toHaveBeenLastCalledWith(true);
    });

    it("stays in Edit when a document cannot be resolved (a paused conflict or declined prompt)", async () => {
        const { ports, app } = fakePorts({
            dialogueLive: fakeLive(true),
            resolveDocument: vi.fn(async () => false),
        });
        const c = createModeController("edit", ports);
        app.setEditable.mockClear();

        c.switchTo("view");
        await flush();

        expect(c.mode).toBe("edit");
        expect(ports.resolveDocument).toHaveBeenCalledOnce();
        expect(app.setEditable).not.toHaveBeenCalled();
        expect(ports.dialogueLive.discard).not.toHaveBeenCalled();
    });

    it("resolves each document and switches when navigation is permitted", async () => {
        const { ports, app } = fakePorts({
            dialogueLive: fakeLive(true),
            resolveDocument: vi.fn(async () => true),
        });
        const c = createModeController("edit", ports);

        c.switchTo("view");
        await flush();

        expect(c.mode).toBe("view");
        expect(ports.dialogueLive.discard).toHaveBeenCalledOnce();
        expect(app.setEditable).toHaveBeenLastCalledWith(false);
    });

    it("ignores a switch to the current mode", () => {
        const { ports } = fakePorts();
        const c = createModeController("view", ports);
        ports.reflect = vi.fn();

        c.switchTo("view");

        expect(ports.dialogueLive.discard).not.toHaveBeenCalled();
    });

    it("makes both the Source and config editors editable in Edit", () => {
        const { ports, app } = fakePorts({ configLive: fakeLive() });
        const c = createModeController("view", ports);

        c.switchTo("edit");

        expect(app.setEditable).toHaveBeenLastCalledWith(true);
        expect(app.setConfigEditable).toHaveBeenLastCalledWith(true);
    });

    it("routes config edits to the config state machine only in Edit", () => {
        const configLive = fakeLive();
        const { ports } = fakePorts({ configLive });
        const c = createModeController("view", ports);

        c.onConfigEditorChange("x = 1"); // View: ignored
        expect(configLive.onEdit).not.toHaveBeenCalled();

        c.switchTo("edit");
        c.onConfigEditorChange("y = 2");
        expect(configLive.onEdit).toHaveBeenCalledWith("y = 2");
    });

    it("resolves both documents before leaving Edit and discards each", async () => {
        const configLive = fakeLive(true);
        const resolveDocument = vi.fn(async () => true);
        const { ports } = fakePorts({ configLive, resolveDocument });
        const c = createModeController("edit", ports);

        c.switchTo("view");
        await flush();

        expect(resolveDocument).toHaveBeenCalledTimes(2);
        expect(configLive.discard).toHaveBeenCalledOnce();
        expect(ports.dialogueLive.discard).toHaveBeenCalledOnce();
        expect(c.mode).toBe("view");
    });

    it("cancels the Edit → View transition when Edit is reasserted mid-flush", async () => {
        // The writer clicks View, then clicks back into Edit while the flush is still awaiting. The
        // pending transition must not drop into View once the flush resolves — Edit is reasserted.
        let releaseResolve: (ok: boolean) => void = () => {};
        const resolveDocument = vi.fn(
            () => new Promise<boolean>((resolve) => (releaseResolve = resolve)),
        );
        const { ports, app } = fakePorts({
            dialogueLive: fakeLive(true),
            resolveDocument,
        });
        const c = createModeController("edit", ports);
        app.setEditable.mockClear();

        c.switchTo("view"); // begins the async leave-Edit
        await flush(); // let it reach the awaited resolveDocument
        c.switchTo("edit"); // reassert Edit while the flush is in flight
        releaseResolve(true); // the flush now settles
        await flush();

        expect(c.mode).toBe("edit"); // the stale View transition was cancelled
        expect(ports.dialogueLive.discard).not.toHaveBeenCalled();
        expect(app.setEditable).not.toHaveBeenCalledWith(false);
    });

    it("lets the latest Edit → View transition win when a newer switch supersedes an in-flight one", async () => {
        const releasers: Array<(ok: boolean) => void> = [];
        const resolveDocument = vi.fn(
            () => new Promise<boolean>((resolve) => releasers.push(resolve)),
        );
        const { ports } = fakePorts({
            dialogueLive: fakeLive(true),
            resolveDocument,
        });
        const c = createModeController("edit", ports);

        c.switchTo("view"); // first transition
        await flush();
        c.switchTo("edit"); // cancel it (back to Edit)
        c.switchTo("view"); // a fresh transition supersedes
        await flush();

        // Settle every awaited resolve; only the latest transition may apply View, exactly once.
        releasers.forEach((release) => release(true));
        await flush();

        expect(resolveDocument).toHaveBeenCalledTimes(2);
        expect(c.mode).toBe("view");
        expect(ports.dialogueLive.discard).toHaveBeenCalledOnce();
    });
});
