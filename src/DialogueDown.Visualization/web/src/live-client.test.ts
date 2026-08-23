import { describe, it, expect, vi } from "vitest";
import { watchServerEvents } from "./live-client";

/** A stand-in for the browser's EventSource (jsdom has none). */
class FakeEventSource {
    private readonly handlers: Record<string, (event: MessageEvent) => void> = {};
    closed = false;
    addEventListener(type: string, handler: (event: MessageEvent) => void): void {
        this.handlers[type] = handler;
    }
    close(): void {
        this.closed = true;
    }
    emit(type: string, data: string): void {
        // A closed EventSource delivers nothing, so neither does the fake.
        if (this.closed) return;
        this.handlers[type]?.(new MessageEvent(type, { data }));
    }
}

function setup() {
    const handlers = {
        onReload: vi.fn(),
        onReloadConfig: vi.fn(),
        onProblem: vi.fn(),
        onDisplaced: vi.fn(),
    };
    const source = new FakeEventSource();
    const returned = watchServerEvents(handlers, null, () => source as unknown as EventSource);
    return { handlers, source, returned };
}

describe("watchServerEvents", () => {
    it("routes a reload event's report to onReload", () => {
        const { handlers, source } = setup();
        const report = { path: "s.dialogue.md", source: "# Hi", stages: [] };

        source.emit("reload", JSON.stringify(report));

        expect(handlers.onReload).toHaveBeenCalledWith(report);
        expect(handlers.onProblem).not.toHaveBeenCalled();
    });

    it("routes a reload-config event's report to onReloadConfig", () => {
        const { handlers, source } = setup();
        const report = { path: "s.dialogue.md", stages: [], outcome: "loaded" };

        source.emit("reload-config", JSON.stringify(report));

        expect(handlers.onReloadConfig).toHaveBeenCalledWith(report);
        expect(handlers.onReload).not.toHaveBeenCalled();
    });

    it("routes a problem event's message to onProblem", () => {
        const { handlers, source } = setup();

        source.emit("problem", JSON.stringify({ message: "compile error at line 3" }));

        expect(handlers.onProblem).toHaveBeenCalledWith("compile error at line 3", undefined);
        expect(handlers.onReload).not.toHaveBeenCalled();
    });

    it("forwards a disk problem's target so it can route to the matching controller", () => {
        const { handlers, source } = setup();

        source.emit(
            "problem",
            JSON.stringify({ message: "Configuration not found: dialogue.toml", target: "config" }),
        );

        expect(handlers.onProblem).toHaveBeenCalledWith(
            "Configuration not found: dialogue.toml",
            "config",
        );
    });

    it("exposes the event source it connected to", () => {
        const { source, returned } = setup();
        expect(returned.source).toBe(source);
    });

    it("opens a real EventSource at /api/events by default", () => {
        const created: string[] = [];
        const RealEventSource = globalThis.EventSource;
        // @ts-expect-error - install a minimal fake constructor for the default path
        globalThis.EventSource = class {
            constructor(url: string) {
                created.push(url);
            }
            addEventListener(): void {}
        };
        try {
            watchServerEvents({
                onReload: vi.fn(),
                onReloadConfig: vi.fn(),
                onProblem: vi.fn(),
                onDisplaced: vi.fn(),
            });
            expect(created).toEqual(["/api/events"]);
        } finally {
            globalThis.EventSource = RealEventSource;
        }
    });
});

describe("watchServerEvents — following the active document", () => {
    /** Watch through a factory that records every stream it hands out. */
    function watchRecording(onReload: (source: string) => void = () => {}) {
        const sources: FakeEventSource[] = [];
        const urls: string[] = [];
        const watch = watchServerEvents(
            {
                onReload: (report) => onReload(report.source ?? ""),
                onReloadConfig: vi.fn(),
                onProblem: vi.fn(),
                onDisplaced: vi.fn(),
            },
            "a.dialogue.md",
            (url) => {
                urls.push(url);
                const source = new FakeEventSource();
                sources.push(source);
                return source as unknown as EventSource;
            },
        );
        return { sources, urls, watch };
    }

    it("resubscribes on demand, so the stream follows the script now open", () => {
        // The server binds a stream to whichever document was active when it opened, so after a
        // switch the old stream is listening to a session nobody is looking at any more.
        const { sources, watch } = watchRecording();

        watch.resubscribe("b.dialogue.md");

        expect(sources).toHaveLength(2);
        expect(sources[0].closed).toBe(true);
        expect(sources[1].closed).toBe(false);
    });

    it("routes events from the stream it resubscribed to", () => {
        const seen: string[] = [];
        const { sources, watch } = watchRecording((source) => seen.push(source));

        watch.resubscribe("b.dialogue.md");
        sources[1].emit("reload", JSON.stringify({ source: "# Opened", stages: [] }));

        expect(seen).toEqual(["# Opened"]);
    });

    it("names the document it is showing, so the server binds the stream to that script", () => {
        // Without a name the server can only bind to whatever is active, which after somebody
        // else opens a script is a different document than this tab is displaying.
        const { urls } = watchRecording();

        expect(urls[0]).toBe("/api/events?doc=a.dialogue.md");
    });

    it("names the newly opened document when it resubscribes", () => {
        const { urls, watch } = watchRecording();

        watch.resubscribe("b.dialogue.md");

        expect(urls[1]).toBe("/api/events?doc=b.dialogue.md");
    });

    it("ignores a late event from the stream it left behind", () => {
        const seen: string[] = [];
        const { sources, watch } = watchRecording((source) => seen.push(source));

        watch.resubscribe("b.dialogue.md");
        sources[0].emit("reload", JSON.stringify({ source: "# Stale", stages: [] }));

        expect(seen).toEqual([]);
    });
});

describe("watchServerEvents — displacement", () => {
    it("reports which script stopped being served", () => {
        const { handlers, source } = setup();

        source.emit("displaced", JSON.stringify({ document: "a.dialogue.md" }));

        expect(handlers.onDisplaced).toHaveBeenCalledWith("a.dialogue.md");
    });

    it("closes the stream, so the browser does not reconnect it to another document", () => {
        // An EventSource reconnects a stream that merely ends. Reconnecting would bind this tab
        // to whichever document is active now, and it would start applying that script's reloads
        // to what it is showing — worse than the silence it replaced.
        const { source } = setup();

        source.emit("displaced", JSON.stringify({ document: "a.dialogue.md" }));

        expect(source.closed).toBe(true);
    });

    it("delivers nothing more once displaced", () => {
        const { handlers, source } = setup();

        source.emit("displaced", JSON.stringify({ document: "a.dialogue.md" }));
        source.emit("reload", JSON.stringify({ source: "# Another script", stages: [] }));

        expect(handlers.onReload).not.toHaveBeenCalled();
    });
});
