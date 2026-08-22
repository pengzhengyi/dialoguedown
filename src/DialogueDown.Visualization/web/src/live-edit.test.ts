import { describe, it, expect, vi } from "vitest";
import {
    createLiveEdit,
    type DiskLoad,
    type LiveEditPorts,
    type SaveOutcome,
    type SaveRequest,
    type SaveStatus,
} from "./live-edit";
import type { DocumentType } from "./save-mode";
import type { Report } from "./model";

const REPORT: Report = {
    source: "# Saved",
    stages: [{ title: "Markdown AST", description: "", nodes: [], edges: [] }],
};

function reportFor(source: string): Report {
    return { source, stages: REPORT.stages };
}

/** A controllable save port: each call parks until the test resolves it with an outcome. */
function harness(documentType: DocumentType = "source", initial = "# Saved") {
    const saves: Array<{
        request: SaveRequest;
        resolve: (outcome: SaveOutcome) => void;
        reject: (reason?: unknown) => void;
    }> = [];
    const idle: Array<() => void> = [];
    const calls = {
        applied: [] as Report[],
        content: [] as string[],
        dirty: [] as boolean[],
        status: [] as Array<{ status: SaveStatus; message?: string }>,
        unload: [] as boolean[],
    };
    let diskLoad: DiskLoad = { kind: "loaded", source: initial, report: reportFor(initial) };

    const ports: LiveEditPorts = {
        save: (request) =>
            new Promise<SaveOutcome>((resolve, reject) => saves.push({ request, resolve, reject })),
        loadFromDisk: () => Promise.resolve(diskLoad),
        applyReport: (report) => calls.applied.push(report),
        setContent: (source) => calls.content.push(source),
        setDirty: (dirty) => calls.dirty.push(dirty),
        setStatus: (status, message) => calls.status.push({ status, message }),
        setUnloadGuard: (active) => calls.unload.push(active),
        scheduleIdle: (callback) => {
            idle.push(callback);
            return () => {
                const index = idle.indexOf(callback);
                if (index >= 0) idle.splice(index, 1);
            };
        },
    };

    const onModeChange = vi.fn();
    const make = (mode: "auto" | "manual" = "auto") =>
        createLiveEdit(ports, { documentType, mode, onModeChange }, initial);

    return {
        ports,
        calls,
        saves,
        idle,
        onModeChange,
        make,
        setDiskLoad: (load: DiskLoad) => {
            diskLoad = load;
        },
        /** Fire the pending idle callback (as the fake timer would). */
        fireIdle: () => {
            const callback = idle.shift();
            callback?.();
        },
        /** Resolve the oldest pending save with an outcome and let microtasks flush. */
        resolveSave: async (outcome: SaveOutcome) => {
            const pending = saves.shift();
            pending?.resolve(outcome);
            await Promise.resolve();
            await Promise.resolve();
        },
        rejectSave: async (reason?: unknown) => {
            const pending = saves.shift();
            pending?.reject(reason);
            await Promise.resolve();
            await Promise.resolve();
        },
    };
}

const savedOutcome = (source: string): SaveOutcome => ({
    kind: "saved",
    report: reportFor(source),
    source,
});

describe("createLiveEdit — editing and dirty", () => {
    it("marks dirty on the first edit, arms the unload guard, and reports Unsaved", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Edited");

        expect(live.dirty).toBe(true);
        expect(h.calls.dirty.at(-1)).toBe(true);
        expect(h.calls.unload.at(-1)).toBe(true);
        expect(live.status).toBe("dirty");
    });

    it("ignores a no-op edit that matches the buffer", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Saved");

        expect(live.dirty).toBe(false);
        expect(h.calls.status).toEqual([]);
    });
});

describe("createLiveEdit — Auto debounce", () => {
    it("arms an idle save 1s after an edit and saves the latest buffer", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# One");
        live.onEdit("# Two"); // restarts the idle timer; only one callback is pending
        expect(h.idle.length).toBe(1);

        h.fireIdle();
        expect(h.saves[0]!.request.source).toBe("# Two");

        await h.resolveSave(savedOutcome("# Two"));
        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
        expect(h.calls.applied.length).toBe(1);
    });

    it("does not idle-save in Manual mode", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# One");

        expect(h.idle.length).toBe(0);
    });
});

describe("createLiveEdit — explicit save", () => {
    it("saves immediately, applies the report, advances the baseline, and clears dirty", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const done = live.save();
        expect(h.saves[0]!.request.source).toBe("# New");
        expect(live.status).toBe("saving");

        await h.resolveSave(savedOutcome("# New"));
        await expect(done).resolves.toBe("saved");
        expect(live.dirty).toBe(false);
        expect(h.calls.applied).toEqual([reportFor("# New")]);
    });

    it("is a no-op when nothing changed since the last save", async () => {
        const h = harness();
        const live = h.make("manual");

        await expect(live.save()).resolves.toBe("noop");
        expect(h.saves.length).toBe(0);
    });

    it("cancels a pending idle timer when saving explicitly", () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        expect(h.idle.length).toBe(1);
        void live.save();

        expect(h.idle.length).toBe(0);
        expect(h.saves.length).toBe(1);
    });
});

describe("createLiveEdit — single-flight and generation safety", () => {
    it("keeps one edit-during-save dirty, does not apply its report, and follows up in Auto", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# One");
        h.fireIdle();
        live.onEdit("# Two"); // a newer edit while the save is in flight

        await h.resolveSave(savedOutcome("# One"));
        // The save advanced the baseline but must not clear dirty for the newer buffer.
        expect(live.dirty).toBe(true);
        expect(live.status).toBe("dirty");
        expect(h.calls.applied.length).toBe(0);

        // Auto schedules one follow-up idle save for the latest buffer.
        expect(h.idle.length).toBe(1);
        h.fireIdle();
        expect(h.saves[0]!.request.source).toBe("# Two");
        expect(h.saves[0]!.request.expectedBaseline).toBe("# One");
    });

    it("never runs two saves at once — a save during an in-flight save is queued", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# One");
        void live.save();
        live.onEdit("# Two");
        void live.save();

        expect(h.saves.length).toBe(1); // only the first is running
        await h.resolveSave(savedOutcome("# One"));
        expect(h.saves.length).toBe(1); // the queued one runs next
        expect(h.saves[0]!.request.source).toBe("# Two");
    });

    it("shares the in-flight promise for a request with the same identity", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const first = live.save();
        const second = live.save(); // same source, generation, policies → shared

        expect(h.saves.length).toBe(1);
        await h.resolveSave(savedOutcome("# New"));
        await expect(first).resolves.toBe("saved");
        await expect(second).resolves.toBe("saved");
    });

    it("replaces an older queued request with a newer generation and supersedes the older caller", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# One");
        void live.save();
        live.onEdit("# Two");
        const queuedOld = live.save();
        live.onEdit("# Three");
        const queuedNew = live.save();

        await expect(queuedOld).resolves.toBe("superseded");
        await h.resolveSave(savedOutcome("# One"));
        expect(h.saves[0]!.request.source).toBe("# Three");
        await h.resolveSave(savedOutcome("# Three"));
        await expect(queuedNew).resolves.toBe("saved");
    });
});

describe("createLiveEdit — failure, conflict, uncertain", () => {
    it("a failed save stays dirty, reports failure, and clears queued automatic work", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const done = live.save();
        await h.resolveSave({ kind: "failure", message: "disk full" });

        await expect(done).resolves.toBe("failure");
        expect(live.dirty).toBe(true);
        expect(live.status).toBe("error");
    });

    it("a baseline mismatch enters Conflict and cancels queued automatic work", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed on disk" });

        expect(live.status).toBe("conflict");
        expect(h.idle.length).toBe(0);
    });

    it("a transport exception enters Uncertain and clears the queue", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const done = live.save();
        await h.rejectSave(new Error("network"));

        await expect(done).resolves.toBe("uncertain");
        expect(live.status).toBe("uncertain");
    });

    it("a server uncertain outcome enters Uncertain and surfaces its message", async () => {
        // The server could not establish a safe state (a newer external write raced the commit),
        // so it returns an explicit uncertain outcome rather than a plain failure: the controller
        // pauses in Uncertain and announces the server's detail message.
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        const done = live.save();
        await h.resolveSave({ kind: "uncertain", message: "state on disk is uncertain" });

        await expect(done).resolves.toBe("uncertain");
        expect(live.status).toBe("uncertain");
        expect(live.statusMessage).toBe("state on disk is uncertain");
        expect(h.idle.length).toBe(0); // automatic work is cleared, like conflict/failure
    });

    it("editing in Conflict updates the buffer but stays paused", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });
        live.onEdit("# Newer");

        expect(live.status).toBe("conflict");
        expect(h.idle.length).toBe(0); // no auto-retry from an edit in Conflict
    });

    it("Auto does not retry a paused Error state on idle", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "failure", message: "denied" });
        // An edit recovers to Dirty and re-arms idle; without an edit, no retry runs.
        expect(h.idle.length).toBe(0);
    });
});

describe("createLiveEdit — reload from disk", () => {
    it("replaces the buffer with external content and adopts it as the baseline", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        h.setDiskLoad({ kind: "loaded", source: "# External", report: reportFor("# External") });
        await live.reload();

        expect(h.calls.content.at(-1)).toBe("# External");
        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
    });

    it("keeps Conflict when the file was deleted on disk", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        h.setDiskLoad({ kind: "missing" });
        await expect(live.reload()).resolves.toBe("conflict");
        expect(live.status).toBe("conflict");
    });
});

describe("createLiveEdit — Config validation", () => {
    it("Config Auto enters Waiting for valid TOML on an invalid-auto outcome", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("auto");

        live.onEdit("bogus");
        h.fireIdle();
        expect(h.saves[0]!.request.validation).toBe("require-valid");
        await h.resolveSave({ kind: "invalid-auto", message: "bad TOML" });

        expect(live.status).toBe("waiting");
        expect(live.dirty).toBe(true);
    });

    it("Config explicit save uses allow-invalid and reports Saved — invalid TOML", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        live.onEdit("bogus");
        const done = live.save();
        expect(h.saves[0]!.request.validation).toBe("allow-invalid");
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "bad",
        });

        await expect(done).resolves.toBe("saved-invalid");
        expect(live.status).toBe("saved-invalid");
        expect(live.dirty).toBe(false); // persisted, so not dirty
    });

    it("explicit invalid Config that completes after a newer edit advances baseline but stays dirty", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        live.onEdit("bogus");
        const done = live.save();
        live.onEdit("bogus2"); // newer edit during the save
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "bad",
        });

        await expect(done).resolves.toBe("saved-invalid");
        expect(live.dirty).toBe(true); // the newer buffer is still unsaved
    });

    it("from Waiting, an explicit save force-writes the invalid TOML", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("auto");

        live.onEdit("bogus");
        h.fireIdle();
        await h.resolveSave({ kind: "invalid-auto", message: "bad" });
        expect(live.status).toBe("waiting");

        const done = live.save();
        expect(h.saves[0]!.request.validation).toBe("allow-invalid");
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "bad",
        });
        await expect(done).resolves.toBe("saved-invalid");
    });

    it("promotes a queued explicit allow-invalid Config save after an in-flight invalid-auto", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("auto");

        // An Auto idle save is in flight and will settle invalid-auto (require-valid).
        live.onEdit("bogus");
        h.fireIdle();
        expect(h.saves[0]!.request.validation).toBe("require-valid");

        // The writer clicks Save while it is in flight, queuing an explicit allow-invalid write.
        const explicit = live.save();
        const idle = live.whenIdle();

        // The invalid-auto settles into Waiting; the queued explicit must still be promoted and run
        // (persisting the invalid TOML) instead of stranding whenIdle behind a paused state.
        await h.resolveSave({ kind: "invalid-auto", message: "bad TOML" });
        expect(h.saves[0]!.request.validation).toBe("allow-invalid");
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "bad",
        });

        await expect(explicit).resolves.toBe("saved-invalid");
        await expect(idle).resolves.toBeUndefined();
        expect(live.status).toBe("saved-invalid");
    });

    it("initializes saved-invalid (report stale) when the initial Config is persisted invalid", () => {
        const h = harness("config", "bogus");
        const live = createLiveEdit(
            h.ports,
            { documentType: "config", mode: "manual", initialValid: false },
            "bogus",
        );

        // A page that reloaded with a persisted-but-invalid Config starts stale, not clean.
        expect(live.status).toBe("saved-invalid");
        expect(live.dirty).toBe(false);

        // An edit then Discard returns to the saved-invalid baseline, not a phantom valid state.
        live.onEdit("bogus2");
        expect(live.dirty).toBe(true);
        live.discardChanges();
        expect(live.status).toBe("saved-invalid");
        expect(live.dirty).toBe(false);
    });
});

describe("createLiveEdit — saved-invalid baseline message", () => {
    it("restores the exact saved-invalid message after an edit and Discard", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        live.onEdit("bogus");
        const done = live.save();
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus"),
            source: "bogus",
            message: "line 1: bad key",
        });
        await expect(done).resolves.toBe("saved-invalid");
        expect(live.statusMessage).toBe("line 1: bad key");

        // Editing then discarding must land back on the saved-invalid baseline with its message,
        // not a saved-invalid status stripped of the detail the reader needs.
        live.onEdit("bogus2");
        expect(live.status).toBe("dirty");
        live.discardChanges();

        expect(live.status).toBe("saved-invalid");
        expect(live.statusMessage).toBe("line 1: bad key");
    });

    it("restores the saved-invalid message captured by a reload after an edit and Discard", async () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        // Fall into Conflict, then Reload from disk finds invalid content carrying a parse message.
        live.onEdit("bogus");
        void live.save();
        await h.resolveSave({ kind: "conflict", message: "changed" });
        expect(live.status).toBe("conflict");

        h.setDiskLoad({
            kind: "invalid",
            source: "disk-bogus",
            report: reportFor("disk-bogus"),
            message: "disk: bad toml",
        });
        await expect(live.reload()).resolves.toBe("saved-invalid");
        expect(live.statusMessage).toBe("disk: bad toml");

        // The reload's message becomes the baseline message, so a later edit and Discard restores it.
        live.onEdit("disk-bogus2");
        live.discardChanges();
        expect(live.status).toBe("saved-invalid");
        expect(live.statusMessage).toBe("disk: bad toml");
    });

    it("adopts an invalid disk config as saved-invalid with its message and restores it on Discard", () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        // A View hot reload adopts external invalid content as the clean saved-invalid baseline.
        live.adoptDisk("ext-bogus", false, "view: bad toml");
        expect(live.status).toBe("saved-invalid");
        expect(live.statusMessage).toBe("view: bad toml");

        live.onEdit("ext-bogus2");
        live.discardChanges();
        expect(live.status).toBe("saved-invalid");
        expect(live.statusMessage).toBe("view: bad toml");
    });

    it("restores the initial persisted-invalid message after an edit and Discard", () => {
        const h = harness("config", "bogus");
        const live = createLiveEdit(
            h.ports,
            {
                documentType: "config",
                mode: "manual",
                initialValid: false,
                initialMessage: "seeded: bad toml",
            },
            "bogus",
        );

        expect(live.statusMessage).toBe("seeded: bad toml");
        live.onEdit("bogus2");
        live.discardChanges();
        expect(live.status).toBe("saved-invalid");
        expect(live.statusMessage).toBe("seeded: bad toml");
    });
});

describe("createLiveEdit — onDiskChange invalidation", () => {
    it("keeps Conflict when an in-flight save resolves after an external disk change", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle(); // an idle save is now in flight
        expect(h.saves.length).toBe(1);

        // An external change lands while the save is in flight.
        live.onDiskChange();
        expect(live.status).toBe("conflict");

        // The stale save response must not clear the Conflict or install its report.
        await h.resolveSave(savedOutcome("# New"));

        expect(live.status).toBe("conflict");
        expect(h.calls.applied).toEqual([]);
    });

    it("does not install a stale baseline when a reload resolves after an external disk change", async () => {
        const h = harness();
        const live = h.make("auto");
        h.setDiskLoad({ kind: "loaded", source: "# Disk", report: reportFor("# Disk") });

        live.onEdit("# New");
        live.onDiskChange(); // conflict, epoch bumped
        expect(live.status).toBe("conflict");

        // Start a reload, then let another external change land before its response is applied.
        const done = live.reload();
        live.onDiskChange();

        await expect(done).resolves.toBe("superseded");
        expect(live.status).toBe("conflict");
        expect(h.calls.applied).toEqual([]); // the stale disk report was not installed
    });
});

describe("createLiveEdit — discard", () => {
    it("discardChanges restores the last saved baseline and clears dirty", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Edited");
        live.discardChanges();

        expect(h.calls.content.at(-1)).toBe("# Saved");
        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
    });

    it("discardChanges is unavailable (a no-op) while a save is in flight", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# Edited");
        void live.save();
        expect(live.canDiscard).toBe(false);
        live.discardChanges();

        expect(h.calls.content).toEqual([]); // nothing restored while saving
    });

    it("discardChanges is unavailable in Conflict", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        h.fireIdle();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        expect(live.canDiscard).toBe(false);
        live.discardChanges();
        expect(live.status).toBe("conflict");
    });

    it("discardChanges from Error restores the baseline and its saved state", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        void live.save();
        await h.resolveSave({ kind: "failure", message: "denied" });
        expect(live.status).toBe("error");

        live.discardChanges();
        expect(live.status).toBe("saved");
        expect(live.dirty).toBe(false);
    });

    it("reapplies the baseline report's diagnostics and semantic tokens exactly once", () => {
        // Restoring the buffer is a full-document replacement, which drops the source editor's
        // diagnostics squiggles and semantic-token highlighting. Discard must reapply the accepted
        // baseline report so those overlays return, and do it exactly once.
        const h = harness();
        const baseline = reportFor("# Saved");
        const live = createLiveEdit(
            h.ports,
            { documentType: "source", mode: "manual", initialReport: baseline },
            "# Saved",
        );

        live.onEdit("# Edited");
        live.discardChanges();

        expect(h.calls.content.at(-1)).toBe("# Saved");
        expect(h.calls.applied).toEqual([baseline]);
    });

    it("reapplies the report from the last successful save, not the initial one", async () => {
        const h = harness();
        const live = createLiveEdit(
            h.ports,
            { documentType: "source", mode: "manual", initialReport: reportFor("# Saved") },
            "# Saved",
        );

        live.onEdit("# New");
        void live.save();
        await h.resolveSave(savedOutcome("# New")); // advances the baseline and its report

        live.onEdit("# Newer");
        live.discardChanges();

        expect(h.calls.content.at(-1)).toBe("# New");
        // One apply from the save, one from the discard restore — both the saved baseline report.
        expect(h.calls.applied).toEqual([reportFor("# New"), reportFor("# New")]);
    });
});

describe("createLiveEdit — save-mode changes", () => {
    it("Auto → Manual cancels a pending idle timer and persists the choice", () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        expect(h.idle.length).toBe(1);
        live.setMode("manual");

        expect(h.idle.length).toBe(0);
        expect(h.onModeChange).toHaveBeenCalledWith("manual");
        expect(live.mode).toBe("manual");
    });

    it("Manual → Auto schedules the current dirty buffer from Dirty", () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        expect(h.idle.length).toBe(0);
        live.setMode("auto");

        expect(h.idle.length).toBe(1);
    });

    it("Manual → Auto does not resume a paused Conflict", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# New");
        void live.save();
        await h.resolveSave({ kind: "conflict", message: "changed" });
        live.setMode("auto");

        expect(live.status).toBe("conflict");
        expect(h.idle.length).toBe(0);
    });
});

describe("createLiveEdit — navigation flush", () => {
    it("flush resolves noop when there is nothing to save", async () => {
        const h = harness();
        const live = h.make("auto");

        await expect(live.flush()).resolves.toBe("noop");
    });

    it("flush saves the latest generation and resolves saved", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        const done = live.flush();
        expect(h.saves[0]!.request.source).toBe("# New");
        await h.resolveSave(savedOutcome("# New"));

        await expect(done).resolves.toBe("saved");
    });

    it("flush resolves conflict when the write conflicts, so navigation can stay in place", async () => {
        const h = harness();
        const live = h.make("auto");

        live.onEdit("# New");
        const done = live.flush();
        await h.resolveSave({ kind: "conflict", message: "changed" });

        await expect(done).resolves.toBe("conflict");
    });
});

describe("createLiveEdit — queued baseline rebasing", () => {
    it("rebases a queued save onto the confirmed baseline of its predecessor", async () => {
        const h = harness();
        const live = h.make("auto");

        // First edit + explicit save starts an in-flight write expecting the initial baseline.
        live.onEdit("# One");
        void live.save();
        expect(h.saves[0]!.request.expectedBaseline).toBe("# Saved");

        // A newer edit while the first save is in flight queues a follow-up. Its expectedBaseline
        // was captured as the still-old baseline at enqueue time.
        live.onEdit("# Two");
        void live.save();

        // The first save confirms and advances the baseline to "# One".
        await h.resolveSave(savedOutcome("# One"));

        // The queued save now runs — rebased onto the predecessor's confirmed baseline, not the
        // stale one captured when it was queued.
        expect(h.saves[0]!.request.source).toBe("# Two");
        expect(h.saves[0]!.request.expectedBaseline).toBe("# One");
    });
});

describe("createLiveEdit — whenIdle", () => {
    it("resolves immediately when no save is in flight", async () => {
        const h = harness();
        const live = h.make("auto");

        await expect(live.whenIdle()).resolves.toBeUndefined();
    });

    it("resolves only after the in-flight and queued saves settle", async () => {
        const h = harness();
        const live = h.make("manual");

        live.onEdit("# One");
        void live.save();
        live.onEdit("# Two");
        void live.save(); // queued

        let settled = false;
        const idle = live.whenIdle().then(() => {
            settled = true;
        });

        await h.resolveSave(savedOutcome("# One")); // first done, queued promoted
        expect(settled).toBe(false); // the queued save is still in flight
        await h.resolveSave(savedOutcome("# Two"));
        await idle;
        expect(settled).toBe(true);
    });
});

describe("createLiveEdit — adoptDisk (View hot reload)", () => {
    it("adopts an external reload as the clean baseline without marking dirty", () => {
        const h = harness();
        const live = h.make("auto");

        live.adoptDisk("# External");

        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
        // A later edit back to the adopted content is not dirty; an edit away from it is.
        live.onEdit("# External");
        expect(live.dirty).toBe(false);
        live.onEdit("# Different");
        expect(live.dirty).toBe(true);
    });

    it("adopts an invalid external config as a saved-invalid baseline", () => {
        const h = harness("config", "name = 1");
        const live = h.make("manual");

        live.adoptDisk("bogus", false);

        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved-invalid");
    });

    it("advances the baseline report on adopt, so a later Discard reapplies the adopted overlays", () => {
        // A View hot reload adopts external content — and its report — as the clean baseline. A
        // later Edit-session Discard restores the buffer with a full-document replacement (which
        // drops the source editor's overlays), so it must reapply the ADOPTED report, not the
        // stale pre-adopt one.
        const h = harness();
        const live = createLiveEdit(
            h.ports,
            { documentType: "source", mode: "manual", initialReport: reportFor("# Initial") },
            "# Initial",
        );

        live.adoptDisk("# External", true, undefined, reportFor("# External"));
        live.onEdit("# Edited");
        live.discardChanges();

        expect(h.calls.content.at(-1)).toBe("# External");
        expect(h.calls.applied.at(-1)).toEqual(reportFor("# External"));
    });
});

describe("createLiveEdit — adoptSwitch (opening another script)", () => {
    it("adopts the newly opened script as the clean baseline", () => {
        const h = harness();
        const live = h.make("manual");

        live.adoptSwitch("# Another", reportFor("# Another"));

        expect(live.dirty).toBe(false);
        expect(live.status).toBe("saved");
        expect(h.calls.content.at(-1)).toBe("# Another");
        // The adopted script, not the previous one, is what a later edit is measured against.
        live.onEdit("# Another");
        expect(live.dirty).toBe(false);
    });

    it("clears a conflict, because the reader asked for this document change", () => {
        const h = harness();
        const live = h.make("manual");
        live.onEdit("# Mine");
        live.onDiskChange("changed on disk");
        expect(live.status).toBe("conflict");

        live.adoptSwitch("# Another", reportFor("# Another"));

        expect(live.status).toBe("saved");
        expect(live.dirty).toBe(false);
    });

    it("a save that was in flight during the switch cannot write onto the new script", async () => {
        const h = harness();
        const live = h.make("manual");
        live.onEdit("# Mine");
        const saving = live.save();
        expect(h.saves).toHaveLength(1);

        live.adoptSwitch("# Another", reportFor("# Another"));
        h.saves[0].resolve({ kind: "saved", report: reportFor("# Mine") });

        // The server now points at another document, so the reply belongs to a file the report is
        // no longer showing: it must not install "# Mine" as the new script's baseline.
        await expect(saving).resolves.toBe("superseded");
        expect(live.status).toBe("saved");
        live.onEdit("# Another");
        expect(live.dirty).toBe(false);
    });
});

describe("createLiveEdit — reload single-flight and staleness", () => {    it("a stale reload response does not overwrite a buffer edited during the reload", async () => {
        const h = harness();
        const live = h.make("auto");
        live.onEdit("# New");
        await h.resolveSave({ kind: "conflict", message: "changed" }); // enter Conflict
        h.setDiskLoad({ kind: "loaded", source: "# Disk", report: reportFor("# Disk") });

        const reloading = live.reload();
        // The writer keeps typing while the reload response is in flight.
        live.onEdit("# Newer");
        const resolution = await reloading;

        expect(resolution).toBe("superseded");
        // The stale reload never replaced the newer buffer nor its content.
        expect(h.calls.content).not.toContain("# Disk");
        live.onEdit("# Newer");
        expect(live.dirty).toBe(true); // still the writer's newer edit, still paused
    });

    it("a second reload supersedes an in-flight one", async () => {
        const h = harness();
        const live = h.make("auto");
        live.onEdit("# New");
        await h.resolveSave({ kind: "conflict", message: "changed" });

        let resolveFirst: (load: DiskLoad) => void = () => {};
        let call = 0;
        h.ports.loadFromDisk = () => {
            call += 1;
            if (call === 1) return new Promise<DiskLoad>((r) => (resolveFirst = r));
            return Promise.resolve({
                kind: "loaded",
                source: "# Second",
                report: reportFor("# Second"),
            });
        };

        const first = live.reload();
        const second = live.reload();
        resolveFirst({ kind: "loaded", source: "# First", report: reportFor("# First") });

        expect(await first).toBe("superseded");
        expect(await second).toBe("saved");
        expect(h.calls.content).toContain("# Second");
        expect(h.calls.content).not.toContain("# First");
    });

    it("a reload cannot overwrite the baseline a newer successful save confirmed", async () => {
        const h = harness();
        const live = h.make("auto");
        live.onEdit("# New");
        await h.resolveSave({ kind: "conflict", message: "changed" }); // enter Conflict

        // The reload's disk fetch is slow (it reads the pre-save content).
        let resolveLoad: (load: DiskLoad) => void = () => {};
        h.ports.loadFromDisk = () => new Promise<DiskLoad>((r) => (resolveLoad = r));
        const reloading = live.reload();

        // While the reload is still fetching, an explicit overwrite save writes and succeeds.
        const saving = live.save({ overwrite: true });
        await h.resolveSave(savedOutcome("# New"));
        expect(await saving).toBe("saved");
        expect(live.status).toBe("saved");

        // The reload now returns the stale pre-save content; it must be discarded, not installed.
        resolveLoad({ kind: "loaded", source: "# Old", report: reportFor("# Old") });
        expect(await reloading).toBe("superseded");
        expect(h.calls.content).not.toContain("# Old");
        expect(live.status).toBe("saved"); // the newer save's state stands
    });
});

describe("createLiveEdit — status detail", () => {
    it("exposes the detail message paired with the current status", async () => {
        const h = harness("config");
        const live = h.make("manual");
        live.onEdit("bogus = true");

        const saving = live.save();
        await h.resolveSave({
            kind: "saved-invalid",
            report: reportFor("bogus = true"),
            source: "bogus = true",
            message: "Unknown key bogus.",
        });
        await saving;

        expect(live.status).toBe("saved-invalid");
        expect(live.statusMessage).toBe("Unknown key bogus."); // detail preserved, not dropped
    });

    it("clears the detail message once the status has none", async () => {
        const h = harness("config");
        const live = h.make("manual");
        live.onEdit('mode = "best-effort"');
        const saving = live.save();
        await h.resolveSave(savedOutcome('mode = "best-effort"'));
        await saving;

        expect(live.status).toBe("saved");
        expect(live.statusMessage).toBeUndefined();
    });

    it("seeds the saved-invalid detail from the initial message", () => {
        const ports = harness("config").ports;
        const live = createLiveEdit(
            ports,
            {
                documentType: "config",
                mode: "manual",
                initialValid: false,
                initialMessage: "Persisted parse error.",
            },
            "broken = ",
        );

        expect(live.status).toBe("saved-invalid");
        expect(live.statusMessage).toBe("Persisted parse error.");
    });
});
