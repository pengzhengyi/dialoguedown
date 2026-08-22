// @vitest-environment node

import { describe, it, expect, vi } from "vitest";
import { createScriptSwitch, type ScriptSwitchPorts } from "./script-switch";
import type { Report } from "./model";

const INITIAL = { path: "intro.dialogue.md", url: "/r/" };

function reportFor(path: string): Report {
    return {
        source: `# ${path}`,
        stages: [],
        path: `/project/${path}`,
        project: { root: "/project", activePath: path },
    };
}

function fakePorts(overrides: Partial<ScriptSwitchPorts> = {}) {
    // The server answers about whichever document was opened last, so the fake payload follows the
    // fake open rather than being pinned to one script.
    let opened = INITIAL.path;
    const ports: ScriptSwitchPorts = {
        resolve: vi.fn(async () => true),
        currentMode: vi.fn(() => "view" as const),
        open: vi.fn(async (path: string) => {
            opened = path;
            return `/r/${path}/`;
        }),
        document: vi.fn(async () => reportFor(opened)),
        fitsPage: vi.fn(() => true),
        apply: vi.fn(),
        pushHistory: vi.fn(),
        setHistory: vi.fn(),
        load: vi.fn(),
        showProblem: vi.fn(),
        ...overrides,
    };
    return ports;
}

/** Let the pending fetches in a switch settle up to the point the test parked them. */
const settle = async () => {
    for (let i = 0; i < 5; i++) await new Promise((resolve) => setTimeout(resolve));
};

const FINALE = "act-1/finale.dialogue.md";

describe("createScriptSwitch", () => {
    it("records the script the page loaded as the first history entry", () => {
        const ports = fakePorts();

        createScriptSwitch(ports, INITIAL);

        // Without this, a Back to the very first script arrives with no state to restore from.
        expect(ports.setHistory).toHaveBeenCalledWith("intro.dialogue.md", "/r/");
    });

    it("opens a script in place and pushes a history entry", async () => {
        const ports = fakePorts();
        const report = reportFor(FINALE);
        (ports.document as ReturnType<typeof vi.fn>).mockResolvedValue(report);

        await createScriptSwitch(ports, INITIAL).open(FINALE);

        expect(ports.open).toHaveBeenCalledWith(FINALE, "view");
        expect(ports.apply).toHaveBeenCalledWith(FINALE, report, "view");
        expect(ports.pushHistory).toHaveBeenCalledWith(FINALE, `/r/${FINALE}/`);
        expect(ports.load).not.toHaveBeenCalled();
    });

    it("opens the script in the mode the reader is already in", async () => {
        const ports = fakePorts({ currentMode: vi.fn(() => "edit" as const) });

        await createScriptSwitch(ports, INITIAL).open(FINALE);

        expect(ports.open).toHaveBeenCalledWith(FINALE, "edit");
        expect(ports.apply).toHaveBeenCalledWith(FINALE, expect.anything(), "edit");
    });

    it("stays put when the reader keeps their unsaved work", async () => {
        const ports = fakePorts({ resolve: vi.fn(async () => false) });

        await createScriptSwitch(ports, INITIAL).open(FINALE);

        expect(ports.open).not.toHaveBeenCalled();
        expect(ports.apply).not.toHaveBeenCalled();
        expect(ports.pushHistory).not.toHaveBeenCalled();
    });

    it("stays on the current script when the server cannot open the new one", async () => {
        const ports = fakePorts({ open: vi.fn(async () => null) });

        await createScriptSwitch(ports, INITIAL).open("gone.dialogue.md");

        expect(ports.showProblem).toHaveBeenCalledWith("Could not open gone.dialogue.md.");
        expect(ports.apply).not.toHaveBeenCalled();
        expect(ports.pushHistory).not.toHaveBeenCalled();
        expect(ports.load).not.toHaveBeenCalled();
    });

    it("falls back to a whole page when the opened script does not fit the page", async () => {
        // A script under a different dialogue.toml compiles in another context, so the page it was
        // built for no longer describes it.
        const ports = fakePorts({ fitsPage: vi.fn(() => false) });

        await createScriptSwitch(ports, INITIAL).open(FINALE);

        expect(ports.load).toHaveBeenCalledWith(`/r/${FINALE}/`);
        expect(ports.apply).not.toHaveBeenCalled();
    });

    it("falls back to a whole page when the payload cannot be fetched", async () => {
        // The server has already switched by then, so a full load is the way back into agreement.
        const ports = fakePorts({ document: vi.fn(async () => null) });

        await createScriptSwitch(ports, INITIAL).open(FINALE);

        expect(ports.load).toHaveBeenCalledWith(`/r/${FINALE}/`);
        expect(ports.apply).not.toHaveBeenCalled();
    });

    it("never repaints a payload for a script other than the one being opened", async () => {
        const ports = fakePorts({ document: vi.fn(async () => reportFor("someone-else.md")) });

        await createScriptSwitch(ports, INITIAL).open(FINALE);

        expect(ports.apply).not.toHaveBeenCalled();
        expect(ports.load).toHaveBeenCalledWith(`/r/${FINALE}/`);
    });

    it("lets the newer of two overlapping switches win", async () => {
        const arrivals: Array<{ path: string; deliver: () => void }> = [];
        let opened = "";
        const ports = fakePorts({
            open: vi.fn(async (path: string) => {
                opened = path;
                return `/r/${path}/`;
            }),
            document: vi.fn(() => {
                const path = opened;
                return new Promise<Report>((resolve) => {
                    arrivals.push({ path, deliver: () => resolve(reportFor(path)) });
                });
            }),
        });
        const scripts = createScriptSwitch(ports, INITIAL);

        const first = scripts.open("slow.dialogue.md");
        await settle();
        const second = scripts.open(FINALE);
        await settle();

        arrivals.find((a) => a.path === FINALE)!.deliver();
        await second;
        arrivals.find((a) => a.path === "slow.dialogue.md")!.deliver(); // arrives late
        await first;

        expect(ports.apply).toHaveBeenCalledTimes(1);
        expect(ports.apply).toHaveBeenCalledWith(FINALE, expect.anything(), "view");
    });

    it("applies a script a Back landed on without pushing another entry", async () => {
        const ports = fakePorts();
        const scripts = createScriptSwitch(ports, INITIAL);
        (ports.setHistory as ReturnType<typeof vi.fn>).mockClear();

        await scripts.restore("intro.dialogue.md");

        expect(ports.apply).toHaveBeenCalledWith("intro.dialogue.md", expect.anything(), "view");
        expect(ports.pushHistory).not.toHaveBeenCalled();
        expect(ports.setHistory).toHaveBeenCalledWith("intro.dialogue.md", "/r/intro.dialogue.md/");
    });

    it("lands a Back in View even when the reader was editing", async () => {
        const ports = fakePorts({ currentMode: vi.fn(() => "edit" as const) });

        await createScriptSwitch(ports, INITIAL).restore("intro.dialogue.md");

        expect(ports.open).toHaveBeenCalledWith("intro.dialogue.md", "view");
        expect(ports.apply).toHaveBeenCalledWith("intro.dialogue.md", expect.anything(), "view");
    });

    it("puts the address bar back when a Back is refused over unsaved work", async () => {
        // The browser moved the address bar before the handler ran, so a refusal has to undo it or
        // the address would name a script the reader is not looking at.
        const ports = fakePorts({ resolve: vi.fn(async () => false) });
        const scripts = createScriptSwitch(ports, INITIAL);
        (ports.setHistory as ReturnType<typeof vi.fn>).mockClear();

        await scripts.restore("act-1/finale.dialogue.md");

        expect(ports.setHistory).toHaveBeenCalledWith("intro.dialogue.md", "/r/");
        expect(ports.apply).not.toHaveBeenCalled();
    });
});
